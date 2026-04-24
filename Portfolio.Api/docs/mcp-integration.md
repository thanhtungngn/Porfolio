# MCP Integration (Phase 1)

This project integrates MCP server tool-calling for the OpenAI provider.

## Scope

- OpenAI provider supports MCP tool discovery and invocation loop.
- Ollama provider remains direct chat fallback (no MCP tool-calling in this phase).

## Configuration

Set in `Portfolio.Api/appsettings.json` (or environment-specific settings):

```json
"Mcp": {
  "Enabled": false,
  "BaseUrl": "",
  "ToolsPath": "/tools",
  "InvokePathTemplate": "/tools/{toolName}",
  "ApiKey": "",
  "TimeoutSeconds": 5
}
```

### Fields

- `Enabled`: Turn MCP integration on/off.
- `BaseUrl`: MCP server base URL.
- `ToolsPath`: Endpoint for tool discovery.
- `InvokePathTemplate`: Endpoint template to invoke tool by name.
- `ApiKey`: Optional bearer token.
- `TimeoutSeconds`: Timeout for MCP requests.

## Detailed Logging Added

Detailed logging is available for:

- MCP tool discovery:
  - start/end
  - endpoint
  - tool count
  - duration

- MCP tool invocation:
  - start/end
  - tool name
  - endpoint
  - arguments length
  - result length
  - duration

- OpenAI tool-calling loop:
  - request start
  - each completion attempt
  - number of tool calls returned
  - per tool-call execution timing
  - final response length

## Notes

- If MCP is disabled or unavailable, the system falls back to normal OpenAI chat flow.
- If MCP invocation fails, warning logs are emitted and tool result fallback text is returned.
