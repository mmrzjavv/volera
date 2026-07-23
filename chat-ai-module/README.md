# Chat AI Module

Production-grade Python AI Agent service with tools, embeddings lifecycle APIs, and speech (TTS/STT). Designed for use behind an API Gateway.

## Features

- **AI Agent**: Reason about user input, decide when to call tools, and return a final answer.
- **Tools** (pluggable): Current date/time, weather (mock). Agent uses them only when needed.
- **Embeddings**: Create, query, re-embed, and delete embeddings (abstract store; default in-memory).
- **Speech**: Text-to-speech and speech-to-text APIs with provider abstraction.
- **LLM**: **Ollama** active by default (model: `gemma3:4b`). **OpenAI** implemented but inactive until enabled via config.

## Requirements

- Python 3.11+
- For agent (default): [Ollama](https://ollama.ai) running locally with a chat model (e.g. `gemma3:4b`).
- For embeddings (widget RAG): Ollama by default with embedding model `znbang/bge:large-en-v1.5-f16`, or OpenAI when configured.

## Setup

```bash
cd chat-ai-module
python -m venv .venv
.venv\Scripts\activate   # Windows
# source .venv/bin/activate  # Unix
pip install -r requirements.txt   # includes asyncpg for Postgres RAG
cp .env.example .env
# Edit .env if needed (defaults: Ollama, no OpenAI key required for chat)
```

## Run

```bash
# From chat-ai-module directory (so config, agent, etc. are importable)
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Ensure Ollama is running and pull the models you need:

```bash
ollama serve
ollama pull gemma3:4b
ollama pull znbang/bge:large-en-v1.5-f16
```

The .NET backend (AI widget) calls this service at `AiService:BaseUrl` (e.g. `http://localhost:8000`). Set `DATABASE_URL` in `.env` to the **same Postgres connection string** as the .NET backend so the widget can read company content from `AiContentBlocks` for RAG.

## API Endpoints

| Method | Path | Description |
|--------|------|-------------|
| GET | /health | Health check |
| POST | /agent/chat | Chat with the agent |
| POST | /embeddings/create | Create and store an embedding |
| POST | /embeddings/reembed | Re-embed and update by id |
| POST | /embeddings/query | Query by text (top_k) |
| DELETE | /embeddings/{id} | Delete embedding |
| POST | **/embed** | **Widget: return embedding for text.** .NET stores it in Postgres (AiContentBlocks). Body: `{ "text" }` → `{ "embedding" }` |
| POST | **/chat** | **Widget: chat with RAG.** Reads AiContentBlocks from Postgres (same DB as .NET). Body: `{ "tenant_id", "message", "session_id" }` → `{ "Answer" }` |
| POST | /text/to-speech | Text → audio |
| POST | /speech/to-text | Audio → text |

### Example: Agent chat

```json
POST /agent/chat
{ "message": "What's the current date and time?" }
```

### Example: Embeddings (requires OpenAI API key for create/query)

```json
POST /embeddings/create
{ "id": "doc1", "text": "Some content", "metadata": {} }

POST /embeddings/query
{ "text": "search query", "top_k": 5 }
```

## Configuration (.env)

| Variable | Default | Description |
|----------|---------|-------------|
| LLM_PROVIDER | ollama | `ollama` or `openai` |
| OLLAMA_BASE_URL | http://localhost:11434 | Ollama server |
| OLLAMA_MODEL | gemma3:4b | Model name |
| OPENAI_API_KEY | (empty) | Required when using OpenAI or embeddings |
| OPENAI_CHAT_MODEL | gpt-4o | Used when LLM_PROVIDER=openai |
| EMBEDDING_PROVIDER | ollama | `ollama` or `openai` for embeddings |
| OLLAMA_EMBEDDING_MODEL | znbang/bge:large-en-v1.5-f16 | Ollama embedding model |
| EMBEDDING_MODEL | text-embedding-3-small | OpenAI embedding model (when EMBEDDING_PROVIDER=openai) |
| MAX_AGENT_STEPS | 10 | Max tool-call rounds per turn |
| LOG_LEVEL | INFO | Logging level |

The service is **Ollama-only by default** (no OpenAI key needed). If the AI widget shows "AI service chat failed", ensure Ollama is running and both models are pulled (see Run section). To use OpenAI: set `LLM_PROVIDER=openai` and `OPENAI_API_KEY`.

## Tests

```bash
pytest tests/ -v
```

## Project structure

- `agent/` — Agent loop, reasoning (LLM + tool selector), tools
- `embeddings/` — Store interface, embedder, in-memory store
- `speech/` — TTS/STT interfaces and placeholder implementations
- `api/` — FastAPI routes and dependencies
- `config/` — Settings from env
- `models/` — Shared DTOs


   cd chat-ai-module
   .venv\Scripts\activate
   uvicorn main:app --reload --host 0.0.0.0 --port 8000
