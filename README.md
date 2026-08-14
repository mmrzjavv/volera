# Volera

Real-time chat, voice calls, and company support widgets.

| App | Path | Stack |
|-----|------|--------|
| API | [`backend-core`](backend-core/) | .NET 9, SignalR, PostgreSQL |
| PWA | [`velora-frontend`](velora-frontend/) | React, Vite |
| Company admin | [`admin-panel`](admin-panel/) | Next.js |
| AI widget | [`chat-ai-module`](chat-ai-module/) | FastAPI, Ollama |

## Setup

**API** — http://localhost:5002

```bash
cd backend-core
dotnet restore VoiceCallApp.sln
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
dotnet run --project src/WebAPI
```

Set `ConnectionStrings:DefaultConnection` and `Jwt:Key` (env or user secrets).

**PWA**

```bash
cd velora-frontend
npm ci
npm run dev
```

Proxies `/api` and SignalR to the API. Optional: `VITE_API_URL`.

**Company admin**

```bash
cd admin-panel
cp .env.local.example .env.local
npm install
npm run dev
```

**AI** (optional) — http://localhost:8000

```bash
cd chat-ai-module
python -m venv .venv
pip install -r requirements.txt
cp .env.example .env
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Point `AiService:BaseUrl` at this service. Use the same Postgres as the API (`DATABASE_URL`).

## Docker

```bash
cd backend-core
cp .env.example .env   # set POSTGRES_PASSWORD
docker compose --env-file .env up -d --build
```

| | |
|--|--|
| App | http://localhost |
| API | http://localhost:5000 |
| Health | `GET /health` |

## Tests

```bash
cd backend-core && dotnet test VoiceCallApp.sln
cd chat-ai-module && pytest tests/ -v
```

## Docs

[Architecture](docs/architecture.md) · [API](docs/api.md) · [Development](docs/development.md) · [Security](docs/security.md) · [Deploy](docs/DEPLOY-SERVER.md)
