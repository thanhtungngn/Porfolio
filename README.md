# Portfolio

A minimal portfolio platform split into two parallel deliverables:

1. **Web app** (React) to show core portfolio information.
2. **Agent API** (ASP.NET Core on .NET 10) to answer deeper questions with either **OpenAI** or **Ollama**.

## Business overview

- Give visitors immediate understanding of profile, skills, and contact channels.
- Allow deeper, conversational exploration through a chatbot on the right side of the page.
- Support cloud (OpenAI) and local (Ollama) LLM choices so teams can switch by cost/privacy needs.

## Technical stack

- Backend: .NET 10, ASP.NET Core Minimal API
- Frontend: React + Vite
- LLM providers: OpenAI Chat Completions API, Ollama `/api/chat`

## Architecture

```text
React SPA (portfolio-web)
  |- GET /api/portfolio  -> basic profile data
  |- POST /api/chat      -> provider router
                            |- OpenAI provider
                            |- Ollama provider

ASP.NET API (Portfolio.Api)
  |- ChatAgentService routes provider requests
  |- Provider implementations call external LLM endpoints
```

## Backend endpoints

- `GET /api/portfolio` returns profile summary, technologies, highlights, and contact info.
- `POST /api/chat` body:

```json
{
  "provider": "openai",
  "model": "gpt-4o-mini",
  "message": "Tell me more about backend projects"
}
```

## Setup

### 1) Start API

```bash
cd Portfolio.Api
dotnet run
```

API runs on `http://localhost:5050` (development profile).

### 2) Configure OpenAI (optional)

Set the API key without committing secrets:

- Environment variable: `OPENAI_API_KEY`
- Or local user secrets:

```bash
cd Portfolio.Api
dotnet user-secrets init
dotnet user-secrets set "ChatProviders:OpenAI:ApiKey" "<your-key>"
```

### 3) Start frontend

```bash
cd portfolio-web
npm install
npm run dev
```

Vite serves on `http://localhost:5173` and proxies `/api` to the backend.

### 4) Use Ollama (optional)

Ensure Ollama is running locally and model configured in:

- `ChatProviders:Ollama:Endpoint` (default `http://localhost:11434/api/chat`)
- `ChatProviders:Ollama:DefaultModel` (default `llama3.2`)
