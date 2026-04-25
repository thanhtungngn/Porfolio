using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using Microsoft.Extensions.Options;
using Portfolio.Api.Features.Chat.Options;

namespace Portfolio.Api.Features.Chat.Services;

internal sealed class McpToolGateway(
    IHttpClientFactory httpClientFactory,
    IOptions<McpOptions> options,
    ILogger<McpToolGateway> logger) : IMcpToolGateway
{
    public async Task<IReadOnlyList<McpToolDefinition>> GetToolsAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            logger.LogDebug("MCP tool discovery skipped. Enabled={Enabled}, BaseUrlConfigured={BaseUrlConfigured}", settings.Enabled, !string.IsNullOrWhiteSpace(settings.BaseUrl));
            return [];
        }

        try
        {
            var endpoint = BuildUri(settings.BaseUrl, settings.ToolsPath);
            logger.LogInformation("MCP tool discovery started. Endpoint={Endpoint}", endpoint);
            var stopwatch = Stopwatch.StartNew();

            using var request = new HttpRequestMessage(ParseHttpMethod(settings.ToolsHttpMethod, HttpMethod.Get), endpoint);
            AddAuthHeader(request, settings.ApiKey);
            AddAcceptHeaders(request);

            if (request.Method == HttpMethod.Post)
            {
                request.Content = new StringContent(
                    "{\"jsonrpc\":\"2.0\",\"id\":\"tools-list\",\"method\":\"tools/list\",\"params\":{}}",
                    Encoding.UTF8,
                    "application/json");
            }

            var responseBody = await SendAsync(request, settings.TimeoutSeconds, cancellationToken);
            if (!TryParseJsonDocument(responseBody, out var doc))
            {
                logger.LogWarning("MCP tool discovery returned non-JSON payload. Falling back without MCP tools.");
                return [];
            }

            using (doc)
            {
                var toolsElement = GetToolsArray(doc.RootElement);
                if (!toolsElement.HasValue || toolsElement.Value.ValueKind != JsonValueKind.Array)
                {
                    return [];
                }

                var tools = new List<McpToolDefinition>();
                foreach (var tool in toolsElement.Value.EnumerateArray())
                {
                    var name = tool.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    var description = tool.TryGetProperty("description", out var descEl)
                        ? descEl.GetString() ?? string.Empty
                        : string.Empty;

                    var schemaJson = tool.TryGetProperty("inputSchema", out var schemaEl)
                        ? schemaEl.GetRawText()
                        : "{\"type\":\"object\",\"properties\":{}}";

                    tools.Add(new McpToolDefinition(name!, description, schemaJson));
                }

                stopwatch.Stop();
                logger.LogInformation("MCP tool discovery completed. ToolCount={ToolCount}, DurationMs={DurationMs}", tools.Count, stopwatch.ElapsedMilliseconds);

                return tools;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "MCP tools discovery failed. Falling back without MCP tools.");
            return [];
        }
    }

    public async Task<string> InvokeToolAsync(string toolName, string? argumentsJson, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.BaseUrl))
        {
            return "MCP is disabled.";
        }

        try
        {
            var invokePath = settings.InvokePathTemplate.Replace("{toolName}", Uri.EscapeDataString(toolName), StringComparison.OrdinalIgnoreCase);
            var endpoint = BuildUri(settings.BaseUrl, invokePath);
            logger.LogInformation(
                "MCP invoke started. ToolName={ToolName}, Endpoint={Endpoint}, ArgumentsLength={ArgumentsLength}",
                toolName,
                endpoint,
                argumentsJson?.Length ?? 0);

            var stopwatch = Stopwatch.StartNew();

            using var request = new HttpRequestMessage(ParseHttpMethod(settings.InvokeHttpMethod, HttpMethod.Post), endpoint);
            AddAuthHeader(request, settings.ApiKey);
            AddAcceptHeaders(request);

            if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
            {
                var normalizedArguments = NormalizeArgumentsJson(argumentsJson);
                request.Content = new StringContent(
                    $"{{\"jsonrpc\":\"2.0\",\"id\":\"tool-call-{Guid.NewGuid():N}\",\"method\":\"tools/call\",\"params\":{{\"name\":{JsonSerializer.Serialize(toolName)},\"arguments\":{normalizedArguments}}}}}",
                    Encoding.UTF8,
                    "application/json");
            }

            var responseBody = await SendAsync(request, settings.TimeoutSeconds, cancellationToken);
            if (!TryParseJsonDocument(responseBody, out var doc))
            {
                stopwatch.Stop();
                logger.LogInformation(
                    "MCP invoke completed with non-JSON payload. ToolName={ToolName}, DurationMs={DurationMs}, ResultLength={ResultLength}",
                    toolName,
                    stopwatch.ElapsedMilliseconds,
                    responseBody.Length);
                return responseBody;
            }

            using (doc)
            {
                stopwatch.Stop();

                if (TryExtractToolContent(doc.RootElement, out var extractedContent))
                {
                    logger.LogInformation(
                        "MCP invoke completed. ToolName={ToolName}, DurationMs={DurationMs}, ResultLength={ResultLength}",
                        toolName,
                        stopwatch.ElapsedMilliseconds,
                        extractedContent.Length);

                    return extractedContent;
                }

                var raw = doc.RootElement.GetRawText();
                logger.LogInformation(
                    "MCP invoke completed with raw payload. ToolName={ToolName}, DurationMs={DurationMs}, ResultLength={ResultLength}",
                    toolName,
                    stopwatch.ElapsedMilliseconds,
                    raw.Length);

                return raw;
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "MCP tool invocation failed for tool {ToolName}.", toolName);
            return $"Tool '{toolName}' invocation failed.";
        }
    }

    private static bool TryParseJsonDocument(string payload, out JsonDocument? document)
    {
        document = null;
        if (string.IsNullOrWhiteSpace(payload))
        {
            return false;
        }

        var trimmed = payload.Trim();
        if ((trimmed.StartsWith('{') || trimmed.StartsWith('[')) && TryParseJson(trimmed, out document))
        {
            return true;
        }

        if (TryExtractJsonFromSse(payload, out var sseJson) && TryParseJson(sseJson, out document))
        {
            return true;
        }

        return false;
    }

    private static bool TryParseJson(string payload, out JsonDocument? document)
    {
        document = null;
        try
        {
            document = JsonDocument.Parse(payload);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static bool TryExtractJsonFromSse(string payload, out string json)
    {
        json = string.Empty;
        var normalized = payload.Replace("\r", string.Empty, StringComparison.Ordinal);
        var lines = normalized.Split('\n');

        for (var i = lines.Length - 1; i >= 0; i--)
        {
            var line = lines[i].Trim();
            if (!line.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var data = line[5..].Trim();
            if (string.IsNullOrWhiteSpace(data) || string.Equals(data, "[DONE]", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (data.StartsWith('{') || data.StartsWith('['))
            {
                json = data;
                return true;
            }
        }

        return false;
    }

    private async Task<string> SendAsync(HttpRequestMessage request, int timeoutSeconds, CancellationToken cancellationToken)
    {
        var client = httpClientFactory.CreateClient();
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, timeoutSeconds)));

        using var response = await client.SendAsync(request, timeoutCts.Token);
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token);

        response.EnsureSuccessStatusCode();
        return body;
    }

    private static void AddAuthHeader(HttpRequestMessage request, string apiKey)
    {
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        }
    }

    private static void AddAcceptHeaders(HttpRequestMessage request)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json") { Quality = 1.0 });
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream") { Quality = 0.5 });
    }

    private static JsonElement? GetToolsArray(JsonElement root)
    {
        if (root.TryGetProperty("tools", out var directTools) && directTools.ValueKind == JsonValueKind.Array)
        {
            return directTools;
        }

        if (root.TryGetProperty("result", out var result)
            && result.ValueKind == JsonValueKind.Object
            && result.TryGetProperty("tools", out var resultTools)
            && resultTools.ValueKind == JsonValueKind.Array)
        {
            return resultTools;
        }

        return null;
    }

    private static bool TryExtractToolContent(JsonElement root, out string content)
    {
        if (root.TryGetProperty("content", out var directContent))
        {
            content = directContent.ValueKind == JsonValueKind.String
                ? directContent.GetString() ?? string.Empty
                : directContent.GetRawText();
            return true;
        }

        if (root.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Object)
        {
            if (result.TryGetProperty("content", out var resultContent) && resultContent.ValueKind == JsonValueKind.String)
            {
                content = resultContent.GetString() ?? string.Empty;
                return true;
            }

            if (result.TryGetProperty("content", out var contentArray) && contentArray.ValueKind == JsonValueKind.Array)
            {
                var lines = new List<string>();
                foreach (var item in contentArray.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object
                        && item.TryGetProperty("text", out var textEl)
                        && textEl.ValueKind == JsonValueKind.String)
                    {
                        var line = textEl.GetString();
                        if (!string.IsNullOrWhiteSpace(line))
                        {
                            lines.Add(line);
                        }
                    }
                }

                if (lines.Count > 0)
                {
                    content = string.Join("\n", lines);
                    return true;
                }

                content = contentArray.GetRawText();
                return true;
            }

            content = result.GetRawText();
            return true;
        }

        content = string.Empty;
        return false;
    }

    private static string NormalizeArgumentsJson(string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return "{}";
        }

        try
        {
            using var argsDoc = JsonDocument.Parse(argumentsJson);
            return argsDoc.RootElement.ValueKind == JsonValueKind.Object
                ? argsDoc.RootElement.GetRawText()
                : "{}";
        }
        catch (JsonException)
        {
            return "{}";
        }
    }

    private static string BuildUri(string baseUrl, string path)
    {
        var left = baseUrl.TrimEnd('/');
        var right = path.StartsWith('/') ? path : $"/{path}";
        return $"{left}{right}";
    }

    private static HttpMethod ParseHttpMethod(string? method, HttpMethod fallback)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            return fallback;
        }

        return method.Trim().ToUpperInvariant() switch
        {
            "GET" => HttpMethod.Get,
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            "PATCH" => HttpMethod.Patch,
            "DELETE" => HttpMethod.Delete,
            "HEAD" => HttpMethod.Head,
            _ => fallback
        };
    }
}
