using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Portfolio.Api.Features.Chat.Contracts;
using Portfolio.Api.Features.Chat.Exceptions;
using Portfolio.Api.Features.Chat.Options;

namespace Portfolio.Api.Features.Chat.Services;

internal sealed class OpenAiChatProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<OpenAiOptions> options,
    IOptions<McpOptions> mcpOptions,
    IMcpToolGateway mcpToolGateway,
    ILogger<OpenAiChatProvider> logger) : IChatProvider
{
    public string Name => "openai";

    public async Task<string> GetReplyAsync(ChatRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var settings = options.Value;
            var apiKey = string.IsNullOrWhiteSpace(settings.ApiKey)
                ? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                : settings.ApiKey;

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ChatValidationException("OpenAI API key is missing. Configure OPENAI_API_KEY or ChatProviders:OpenAI:ApiKey.");
            }

            var messages = new List<Dictionary<string, object?>>
            {
                new() { ["role"] = "system", ["content"] = settings.SystemPrompt },
                new() { ["role"] = "user", ["content"] = request.Message }
            };

            var tools = await BuildToolsAsync(cancellationToken);
            var model = string.IsNullOrWhiteSpace(request.Model) ? settings.DefaultModel : request.Model;

            logger.LogInformation(
                "OpenAI chat request started. Model={Model}, McpToolsEnabled={McpToolsEnabled}, InitialMessageCount={InitialMessageCount}",
                model,
                tools is { Count: > 0 },
                messages.Count);

            for (var attempt = 0; attempt < 4; attempt++)
            {
                logger.LogInformation(
                    "OpenAI completion attempt {Attempt}. MessageCount={MessageCount}, ToolDefinitionCount={ToolDefinitionCount}",
                    attempt + 1,
                    messages.Count,
                    tools?.Count ?? 0);

                using var doc = await SendCompletionAsync(settings.Endpoint, apiKey, model, messages, tools, cancellationToken);
                var message = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

                if (TryGetToolCalls(message, out var toolCalls))
                {
                    logger.LogInformation(
                        "OpenAI returned tool calls. Attempt={Attempt}, ToolCallCount={ToolCallCount}",
                        attempt + 1,
                        toolCalls.Count);

                    messages.Add(new Dictionary<string, object?>
                    {
                        ["role"] = "assistant",
                        ["content"] = message.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String ? c.GetString() : string.Empty,
                        ["tool_calls"] = JsonSerializer.Deserialize<object>(message.GetProperty("tool_calls").GetRawText())
                    });

                    foreach (var toolCall in toolCalls)
                    {
                        var toolStopwatch = Stopwatch.StartNew();
                        logger.LogInformation(
                            "MCP tool call started. ToolName={ToolName}, ToolCallId={ToolCallId}, ArgumentsLength={ArgumentsLength}",
                            toolCall.Name,
                            toolCall.Id,
                            toolCall.ArgumentsJson?.Length ?? 0);

                        var result = await mcpToolGateway.InvokeToolAsync(toolCall.Name, toolCall.ArgumentsJson, cancellationToken);

                        toolStopwatch.Stop();
                        logger.LogInformation(
                            "MCP tool call completed. ToolName={ToolName}, ToolCallId={ToolCallId}, DurationMs={DurationMs}, ResultLength={ResultLength}",
                            toolCall.Name,
                            toolCall.Id,
                            toolStopwatch.ElapsedMilliseconds,
                            result.Length);

                        messages.Add(new Dictionary<string, object?>
                        {
                            ["role"] = "tool",
                            ["tool_call_id"] = toolCall.Id,
                            ["content"] = result
                        });
                    }

                    continue;
                }

                var content = message.TryGetProperty("content", out var contentElement)
                    ? contentElement.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(content))
                {
                    throw new ChatProviderException("OpenAI response did not include a valid reply.");
                }

                logger.LogInformation(
                    "OpenAI chat request completed. Attempt={Attempt}, FinalReplyLength={FinalReplyLength}",
                    attempt + 1,
                    content.Length);

                return content;
            }

            logger.LogWarning("OpenAI tool-calling exceeded maximum attempts.");
            throw new ChatProviderException("OpenAI tool-calling exceeded maximum attempts.");
        }
        catch (HttpRequestException)
        {
            throw new ChatProviderException("OpenAI endpoint is unreachable. Check network and configuration.");
        }
        catch (JsonException)
        {
            throw new ChatProviderException("OpenAI response format was invalid.");
        }
    }

    private async Task<List<object>?> BuildToolsAsync(CancellationToken cancellationToken)
    {
        if (!mcpOptions.Value.Enabled)
        {
            return null;
        }

        var discovered = await mcpToolGateway.GetToolsAsync(cancellationToken);
        if (discovered.Count == 0)
        {
            return null;
        }

        logger.LogInformation("MCP enabled for OpenAI provider. ToolCount={ToolCount}", discovered.Count);

        var tools = new List<object>(discovered.Count);
        foreach (var tool in discovered)
        {
            var parameters = JsonSerializer.Deserialize<JsonElement>(tool.InputSchemaJson);
            tools.Add(new
            {
                type = "function",
                function = new
                {
                    name = tool.Name,
                    description = tool.Description,
                    parameters
                }
            });
        }

        return tools;
    }

    private async Task<JsonDocument> SendCompletionAsync(
        string endpoint,
        string apiKey,
        string model,
        List<Dictionary<string, object?>> messages,
        List<object>? tools,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object?>
        {
            ["model"] = model,
            ["messages"] = messages
        };

        if (tools is { Count: > 0 })
        {
            payload["tools"] = tools;
            payload["tool_choice"] = "auto";
        }

        var client = httpClientFactory.CreateClient();
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, endpoint);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        requestMessage.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await client.SendAsync(requestMessage, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new ChatProviderException($"OpenAI request failed with status {(int)response.StatusCode}: {json}");
        }

        return JsonDocument.Parse(json);
    }

    private static bool TryGetToolCalls(JsonElement message, out List<ToolCall> toolCalls)
    {
        toolCalls = [];
        if (!message.TryGetProperty("tool_calls", out var callsElement) || callsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var call in callsElement.EnumerateArray())
        {
            var id = call.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
            var function = call.TryGetProperty("function", out var fnEl) ? fnEl : default;
            var name = function.ValueKind != JsonValueKind.Undefined && function.TryGetProperty("name", out var nEl) ? nEl.GetString() : null;
            var argumentsJson = function.ValueKind != JsonValueKind.Undefined && function.TryGetProperty("arguments", out var aEl)
                ? aEl.GetString()
                : "{}";

            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            toolCalls.Add(new ToolCall(id!, name!, argumentsJson));
        }

        return toolCalls.Count > 0;
    }

    private sealed record ToolCall(string Id, string Name, string? ArgumentsJson);
}
