# Development guide

## Prerequisites

- .NET 9 SDK
- Node.js 22+ and npm
- PostgreSQL 16+ (or Docker)
- Optional: Python 3.11+, Ollama (AI module)

## Backend (`backend-core`)

```bash
cd backend-core
dotnet restore VoiceCallApp.sln
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
dotnet run --project src/WebAPI
```

- Listen URL: `http://0.0.0.0:5002` (`Properties/launchSettings.json`)
- Swagger typically available in Development (confirm in `Program.cs`)
- Health: `GET /health`, version: `GET /version`
- Hangfire dashboard: `/hangfire` (auth depends on environment configuration)

Configure `ConnectionStrings:DefaultConnection` (and optional Redis/Mongo/Storage/Jwt/AiService) via `appsettings.Development.json`, user secrets, or environment variables. Do not commit secrets.

### Docker stack

```bash
cd backend-core
cp .env.example .env   # set POSTGRES_PASSWORD
docker compose --env-file .env up -d --build
```

- Frontend: http://localhost:80
- API: http://localhost:5000

## Velora frontend

```bash
cd velora-frontend
npm ci
npm run dev
```

Vite proxies `/api`, `/callHub`, `/chatHub`, `/health`, `/version` to `http://localhost:5002`.

**Calls / microphone:** browsers require a secure context. Docker frontend serves **HTTPS** on port **18262** (self-signed). Open `https://<lan-ip>:18262`, accept the certificate warning once, then place calls. HTTP on `18261` redirects to HTTPS. Plain `http://192.168.x.x:18262` will not work for mic/camera on Chrome or Safari (desktop or mobile).

Env names: `VITE_API_URL`, `VITE_ENABLE_MESSAGE_LENGTH_LIMIT`.

## Admin panel

```bash
cd admin-panel
cp .env.local.example .env.local
npm install
npm run dev
```

`NEXT_PUBLIC_API_URL` should point at the API base (no trailing slash), e.g. `http://localhost:5002`.

## Chat AI module

```bash
cd chat-ai-module
python -m venv .venv
# activate, then:
pip install -r requirements.txt
cp .env.example .env
uvicorn main:app --reload --host 0.0.0.0 --port 8000
```

Ensure Ollama models are available when using defaults (`gemma3:4b`, embedding model per README). Set `DATABASE_URL` to the same Postgres as the API. Set backend `AiService:BaseUrl` to `http://localhost:8000`.

## Suggested local ports

| Service | Port |
|---------|------|
| WebAPI (dotnet run) | 5002 |
| WebAPI (compose) | 5000 |
| Velora (vite) | Vite default (often 5173) |
| Velora (compose nginx) | 80 |
| Admin panel | free port starting 3000 |
| AI module | 8000 |

## Changing the backend feature shape

1. Domain entity / interface if needed
2. Command or Query + Validator + Handler
3. Controller endpoint under `api/v1`
4. Migration if schema changed
5. Update the matching frontend client/store/hub handler
6. Add or update tests

## CI/CD

See [DEPLOY-SERVER.md](DEPLOY-SERVER.md) and `.github/workflows/deploy.yml`.
