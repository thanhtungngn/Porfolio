# System Architecture

This document visualizes the current `Porfolio` backend architecture.

## 1. High-Level Context

```mermaid
graph LR
    User[User / Frontend Client]
    API[Portfolio.Api\nASP.NET Core Minimal API]
    OAI[OpenAI API\nChat + Embeddings]
    OLL[Ollama\nLocal LLM]
    MCP[MCP Server\nTool Gateway]
    QDR[Qdrant\nVector Database]
    ASP[Aspire AppHost]

    User --> API
    API --> OAI
    API --> OLL
    API --> MCP
    API --> QDR
    ASP --> API
    ASP --> QDR
```

## 2. Solution Structure

```mermaid
graph TD
    S[Porfolio Solution]
    API[Portfolio.Api]
    TEST[Portfolio.Tests]
    APPHOST[Portfolio.AppHost]
    DEF[Portfolio.ServiceDefaults]

    S --> API
    S --> TEST
    S --> APPHOST
    S --> DEF

    APPHOST --> API
    DEF --> API
    TEST --> API
```

## 3. API Endpoint Map

```mermaid
graph TD
    API[Portfolio.Api]
    P1[GET /api/portfolio]
    C1[POST /api/chat]
    R1[POST /api/rag/ingest]
    R2[POST /api/rag/search]

    API --> P1
    API --> C1
    API --> R1
    API --> R2
```

## 4. Chat Flow (Optional RAG + MCP)

```mermaid
sequenceDiagram
    participant U as User
    participant A as Portfolio.Api
    participant R as RagRetrievalService
    participant E as OpenAI Embedding
    participant Q as Qdrant
    participant C as Chat Provider (OpenAI/Ollama)
    participant M as MCP Gateway

    U->>A: POST /api/chat
    alt useRag = true
        A->>R: GetMatchesAsync(message)
        R->>E: Embed query
        E-->>R: vector
        R->>Q: Search(topK)
        Q-->>R: matches
        R-->>A: context + sources
    end

    A->>C: Send (augmented or raw) prompt
    alt provider=openai and mcp enabled
        C->>M: Discover/Invoke tools
        M-->>C: Tool outputs
    end
    C-->>A: reply
    A-->>U: ChatResponse(reply, sources)
```

## 5. RAG Ingestion Pipeline

```mermaid
flowchart LR
    IN[Input: text or PDF path] --> L{Input type}
    L -->|PDF| P[PdfDocumentLoader]
    L -->|Text| T[Raw Text]
    P --> CH[DocumentChunker]
    T --> CH
    CH --> EMB[OpenAiEmbeddingService]
    EMB --> VEC[1536-d vectors]
    VEC --> VS[QdrantVectorStore.UpsertAsync]
    VS --> COL[(Qdrant Collection)]
```

## 6. Deployment / Runtime View

```mermaid
graph LR
    DEV[Developer Machine]
    APPHOST[Portfolio.AppHost]
    API[Portfolio.Api]
    QDR[Qdrant]
    EXT1[OpenAI]
    EXT2[Ollama]
    EXT3[MCP Server]

    DEV --> APPHOST
    APPHOST --> API
    APPHOST --> QDR
    API --> EXT1
    API --> EXT2
    API --> EXT3
```

## Notes

- OpenAPI and Scalar docs are enabled in Development.
- `/api/rag/ingest` requires `X-API-Key`.
- Chat endpoint is rate-limited.
- Observability is provided via `Portfolio.ServiceDefaults` (OpenTelemetry + health checks).
