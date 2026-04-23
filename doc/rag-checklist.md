# RAG Implementation Checklist (Free Tier)

## Stack
| Layer | Choice | Cost |
|---|---|---|
| LLM | Google Gemini Flash 2.0 | Free (1,500 req/day) |
| Embeddings | Gemini `text-embedding-004` | Free |
| Vector DB | Qdrant (in-memory / free cloud) | Free (1GB) |
| Document Parsing | `PdfPig` | Open source |
| Orchestration | `Microsoft.SemanticKernel` | Open source |

---

## Phase 1 — Setup ✅

- [x] Add NuGet packages:
  - [x] `Microsoft.SemanticKernel` (already present)
  - [x] `Qdrant.Client` — Qdrant vector DB client
  - [x] `PdfPig` — PDF document parsing
- [x] Add `GeminiOptions` configuration model
- [x] Add `QdrantOptions` configuration model
- [x] Register `GeminiEmbeddingService` in DI
- [x] Register `QdrantVectorStore` in DI
- [x] Add config sections to `appsettings.json`:
  - [x] `Rag:Gemini` (ApiKey, EmbeddingModel, EmbeddingEndpoint)
  - [x] `Rag:Qdrant` (Host, Port, CollectionName)

---

## Phase 2 — Ingestion Pipeline ⬜

- [ ] Create `DocumentChunker` — splits text into ~500 token chunks with overlap
- [ ] Create `PdfDocumentLoader` — loads and extracts text from PDFs using PdfPig
- [ ] Create `IngestionService` — orchestrates load → chunk → embed → store
- [ ] Add `POST /api/rag/ingest` endpoint
- [ ] Test ingestion with a sample document

---

## Phase 3 — Retrieval API ⬜

- [ ] Create `RetrievalService` — embeds query, searches Qdrant top-5 chunks
- [ ] Wire retrieval into `/api/chat` as context injection
- [ ] Add `RagChatProvider` as a third provider option (`"rag"`)

---

## Phase 4 — Generation ⬜

- [ ] Build context-aware prompt template
- [ ] Send `[retrieved context] + [user query]` to Gemini Flash 2.0
- [ ] Stream response back via `/api/chat`

---

## Phase 5 — Frontend Hook ⬜

- [ ] Add chat widget to portfolio UI
- [ ] Connect widget to `POST /api/chat` with `provider: "rag"`
- [ ] Show sources/citations from retrieved chunks (optional)
