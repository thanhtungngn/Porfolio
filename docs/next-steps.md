# Next Steps Recommendations

This document summarizes recommended follow-up work after reviewing the current solution.

## 1) Security (Do First)

1. Rotate exposed secrets immediately.
   - `Portfolio.Api/appsettings.Development.json` currently contains real-looking API keys.
   - Revoke and regenerate:
     - OpenAI API key
     - Qdrant API key
2. Move all secrets to User Secrets or environment variables.
3. Add/verify `.gitignore` rules for local secret files.
4. Add a secret-scanning check in CI (e.g., GitHub Advanced Security / Gitleaks).

## 2) Consolidate Duplicate RAG Folders

There are two RAG trees in the workspace:

- `Portfolio.Api/Features/Rag/*` (actively used)
- `Portfolio.Api/Rag/*` (legacy/duplicate artifacts)

Actions:

1. Confirm no references to `Portfolio.Api/Rag/*` remain.
2. Remove the legacy folder to avoid confusion.
3. Keep a single source of truth under `Features/Rag`.

## 3) API Contract and Docs Hardening

1. Add endpoint-level OpenAPI metadata (`WithSummary`, `WithDescription`, response types).
2. Add concrete request/response examples for:
   - `/api/chat`
   - `/api/rag/ingest`
   - `/api/rag/search`
3. Document `X-API-Key` requirement for ingest endpoint directly in OpenAPI.

## 4) RAG Quality Improvements

1. Add chunking strategy config (`chunk size`, `overlap`) via options.
2. Add metadata filters in retrieval (source, tags, date).
3. Add hybrid retrieval plan (keyword + vector) if relevance needs improve.
4. Add evaluation harness for retrieval quality (precision@k / human eval set).

## 5) Reliability and Performance

1. Add retries + timeout policies around Qdrant and OpenAI calls.
2. Add cancellation and request size limits for ingest routes.
3. Add idempotency behavior for repeated ingest of same source.
4. Add background ingestion mode for large files (queue + worker).

## 6) Testing Expansion

Current tests pass (`Portfolio.Tests`). Next additions:

1. Endpoint integration tests (`WebApplicationFactory`) for:
   - auth failure/success on `/api/rag/ingest`
   - `/api/chat` with and without `useRag`
2. Contract tests for MCP tool discovery/invocation behavior.
3. Negative-path tests (Qdrant unavailable, OpenAI timeout, invalid payloads).

## 7) Delivery / Ops

1. Add deployment profiles for API + AppHost target environments.
2. Add health probes for dependencies (Qdrant/OpenAI connectivity where possible).
3. Add dashboards/alerts for:
   - chat latency
   - ingest error rate
   - retrieval hit rate

## Suggested Priority Plan

- **Week 1:** Security rotation + secret management + remove duplicate folders
- **Week 2:** Integration tests + OpenAPI enhancements
- **Week 3:** RAG quality tuning + reliability policies
- **Week 4:** Deployment hardening + observability alerts
