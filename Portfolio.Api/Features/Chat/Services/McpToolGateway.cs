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

            using var request = new HttpRequestMessage(HttpMethod.Get, endpoint);
            AddAuthHeader(request, settings.ApiKey);

            var json = await SendAsync(request, settings.TimeoutSeconds, cancellationToken);
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("tools", out var toolsElement) || toolsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var tools = new List<McpToolDefinition>();
            foreach (var tool in toolsElement.EnumerateArray())
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

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            AddAuthHeader(request, settings.ApiKey);

            request.Content = new StringContent(
                string.IsNullOrWhiteSpace(argumentsJson) ? "{}" : argumentsJson,
                Encoding.UTF8,
                "application/json");

            var json = await SendAsync(request, settings.TimeoutSeconds, cancellationToken);
            using var doc = JsonDocument.Parse(json);

            stopwatch.Stop();

            if (doc.RootElement.TryGetProperty("content", out var contentEl))
            {
                var content = contentEl.ValueKind == JsonValueKind.String
                    ? contentEl.GetString() ?? string.Empty
                    : contentEl.GetRawText();

                logger.LogInformation(
                    "MCP invoke completed. ToolName={ToolName}, DurationMs={DurationMs}, ResultLength={ResultLength}",
                    toolName,
                    stopwatch.ElapsedMilliseconds,
                    content.Length);

                return content;
            }

            var raw = doc.RootElement.GetRawText();
            logger.LogInformation(
                "MCP invoke completed with raw payload. ToolName={ToolName}, DurationMs={DurationMs}, ResultLength={ResultLength}",
                toolName,
                stopwatch.ElapsedMilliseconds,
                raw.Length);

            return raw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            logger.LogWarning(ex, "MCP tool invocation failed for tool {ToolName}.", toolName);
            return $"Tool '{toolName}' invocation failed.";
        }
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

    private static string BuildUri(string baseUrl, string path)
    {
        var left = baseUrl.TrimEnd('/');
        var right = path.StartsWith('/') ? path : $"/{path}";
        return $"{left}{right}";
    }
}
