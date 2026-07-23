# Chat-App-DotNet

Real-time chat and voice-call platform with a .NET API, React PWA, company widget admin, and optional Python AI service.

## Repository layout

| Directory | Description |
|-----------|-------------|
| [`backend-core/`](backend-core/) | ASP.NET Core Web API, SignalR hubs, EF Core, Hangfire (solution: `VoiceCallApp.sln`) |
| [`velora-frontend/`](velora-frontend/) | End-user React + Vite PWA (chat, calls, platform admin UI) |
| [`admin-panel/`](admin-panel/) | Next.js company chat-widget / support admin |
| [`chat-ai-module/`](chat-ai-module/) | FastAPI AI agent + embeddings/RAG for the AI widget |
| [`docs/`](docs/) | Architecture, development, testing, security, API, and deploy guides |
| `publish/`, `publish-chat/` | Published build outputs (not primary source) |

AI agent instructions: [`AGENTS.md`](AGENTS.md).

## Prerequisites

- .NET **9** SDK
- Node.js **22** (matches CI) and npm
- PostgreSQL 16+ (local or Docker)
- Optional: Python 3.11+, [Ollama](https://ollama.ai) (for `chat-ai-module`)
- Optional: Docker / Docker Compose (full stack)

## Quick start (local)

### 1. Backend

```bash
cd backend-core
dotnet restore VoiceCallApp.sln
# Set ConnectionStrings:DefaultConnection in src/WebAPI/appsettings.Development.json (or user secrets)
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
dotnet run --project src/WebAPI
```

API listens on **http://localhost:5002** (see `src/WebAPI/Properties/launchSettings.json`).

### 2. End-user frontend

```bash
cd velora-frontend
npm ci
npm run dev
```

Vite proxies `/api` and hubs to `http://localhost:5002`. Optional env: `VITE_API_URL`.

### 3. Company admin panel

```bash
cd admin-panel
cp .env.local.example .env.local   # set NEXT_PUBLIC_API_URL=http://localhost:5002
npm install
npm run dev
```

### 4. AI module (optional, for AI widget)

```bash
cd chat-ai-module
python -m venv .venv
# activate venv, then:
pip install -r requirements.txt
cp .env.example .env               # set DATABASE_URL to the same Postgres as the API
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Point backend `AiService:BaseUrl` at `http://localhost:8000`.

## Docker (API + Postgres + Velora)

```bash
cd backend-core
cp .env.example .env   # set POSTGRES_PASSWORD
docker compose --env-file .env up -d --build
```

- Frontend: http://localhost (port 80; proxies API/hubs)
- API direct: http://localhost:5000
- Health: `GET /health`

## Tests

```bash
cd backend-core && dotnet test VoiceCallApp.sln
cd chat-ai-module && pytest tests/ -v
```

Frontend packages currently expose `lint` / `build` scripts, not a dedicated unit-test runner.

## Architecture

See [docs/architecture.md](docs/architecture.md). High level:

- Backend: Clean Architecture + CQRS (MediatR) + SignalR
- Velora: React SPA talking to `/api/v1` + `chatHub` / `callHub`
- Admin panel: company (`X-Company-Token`) and support (Bearer) clients
- AI module: FastAPI service used by company AI widget / RAG

## Deploy

Server and GitHub Actions setup: [docs/DEPLOY-SERVER.md](docs/DEPLOY-SERVER.md).  
Workflow: [`.github/workflows/deploy.yml`](.github/workflows/deploy.yml) (build on `main`, SSH + `docker compose`).

## Documentation index

- [AGENTS.md](AGENTS.md) — rules for AI coding agents
- [docs/architecture.md](docs/architecture.md)
- [docs/development.md](docs/development.md)
- [docs/testing.md](docs/testing.md)
- [docs/security.md](docs/security.md)
- [docs/api.md](docs/api.md)
