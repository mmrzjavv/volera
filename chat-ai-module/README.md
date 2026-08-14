# AI module

FastAPI service for the company AI widget (chat + RAG) and a small tool-using agent. Default LLM is Ollama.

```bash
python -m venv .venv
pip install -r requirements.txt
cp .env.example .env
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Pull models, then keep Ollama running:

```bash
ollama pull gemma3:4b
ollama pull znbang/bge:large-en-v1.5-f16
```

Set `DATABASE_URL` to the same Postgres as the API. Set backend `AiService:BaseUrl` to `http://localhost:8000`.

| Method | Path | |
|--------|------|--|
| GET | `/health` | |
| POST | `/chat` | Widget RAG. `{ tenant_id, message, session_id }` |
| POST | `/embed` | Embedding for ingest. `{ text }` |
| POST | `/agent/chat` | Agent. `{ message }` |
| POST | `/embeddings/create` `/query` `/reembed` | Vector store |
| DELETE | `/embeddings/{id}` | |
| POST | `/text/to-speech` `/speech/to-text` | |

| Env | Default |
|-----|---------|
| `LLM_PROVIDER` | `ollama` |
| `OLLAMA_MODEL` | `gemma3:4b` |
| `EMBEDDING_PROVIDER` | `ollama` |
| `DATABASE_URL` | same DB as the API |
| `LLM_PROVIDER=openai` | needs `OPENAI_API_KEY` |

```bash
pytest tests/ -v
```
