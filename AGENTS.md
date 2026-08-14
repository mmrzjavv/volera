# AGENTS.md — Chat-App-DotNet

Instructions for AI coding agents working in this repository.

## Project overview

Multi-product real-time chat / voice-call platform:

| Path | Role | Stack |
|------|------|-------|
| `backend-core/` | Main API, SignalR, EF Core, Hangfire | .NET **9** (`net9.0`), ASP.NET Core, MediatR CQRS, PostgreSQL |
| `velora-frontend/` | End-user chat/call PWA (+ platform admin UI) | React 19, Vite 7, TypeScript, Zustand, Tailwind 4, SignalR |
| `admin-panel/` | Company widget / support SaaS admin | Next.js 14 App Router, React 18, Zustand, Tailwind 3 |
| `chat-ai-module/` | AI agent + RAG/embeddings for company AI widget | Python 3.11+, FastAPI, Ollama (default) |
| `docs/` | Deploy and agent-oriented docs | Markdown |
| `publish/`, `publish-chat/` | Published backend artifacts | **Do not edit** unless explicitly asked |

Product surface includes 1:1 and group chat, voice/group calls, guest chat, company support widget, AI widget (RAG), web push, and platform admin.

There is **no root solution** that covers frontends. Backend solution: `backend-core/VoiceCallApp.sln`.

## Architecture (backend)

Clean Architecture / modular monolith:

```
Shared ← Core.Domain ← Core.Application ← Infrastructure
                            ↑                    ↑
                         WebAPI ─────────────────┘
```

- **Domain** (`src/Core.Domain`): entities, domain interfaces, events/value objects.
- **Application** (`src/Core.Application`): Commands, Queries, Handlers, Validators, DTOs, MediatR behaviors, Administration CQRS.
- **Infrastructure** (`src/Infrastructure`): `ApplicationDbContext`, migrations, repositories, JWT/BCrypt, Redis/S3/etc.
- **WebAPI** (`src/WebAPI`): thin controllers, hubs, middleware, DI wiring.
- **Shared**: `BaseEntity` and shared primitives.

**Dependency rule:** Domain must not reference Infrastructure or WebAPI. Prefer new features as Command/Query + Handler + FluentValidation validator + thin controller.

API routes use manual versioning prefix `api/v1/...` (not Asp.Versioning). Controllers return `ApiResponse<T>` via `ControllerBaseExtensions` (`Success`, `Fail`, `ApiNotFound`, …).

### SignalR hubs

| Hub | Path | Auth |
|-----|------|------|
| CallHub | `/callHub` | JWT |
| ChatHub | `/chatHub` | JWT |
| GuestHub | `/guestHub` | Anonymous |
| CompanyWidgetHub | `/companyWidgetHub` | Anonymous |
| SupportHub | `/supportHub` | SupportUser JWT |
| AiWidgetHub | `/aiWidgetHub` | Anonymous |

Hub JWT is accepted from query `access_token`.

## Frontend roles (do not confuse)

- **`velora-frontend`**: consumer app + **platform** admin at `/admin/*` (Bearer user JWT, axios).
- **`admin-panel`**: **company** widget SaaS (company token `X-Company-Token`) and **support** portal (SupportUser Bearer + `/supportHub`).

## Commands (verified)

### Backend (`backend-core/`)

```bash
dotnet restore VoiceCallApp.sln
dotnet build VoiceCallApp.sln
dotnet test VoiceCallApp.sln
dotnet run --project src/WebAPI
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
```

Dev URL from `launchSettings.json`: `http://0.0.0.0:5002` (Vite proxy targets `localhost:5002`).

Docker (from `backend-core/`, requires `.env` with `POSTGRES_PASSWORD`):

```bash
docker compose --env-file .env up -d --build
```

Compose runs `webapi` (:5000→8080), `postgres:16-alpine`, and builds `../velora-frontend` on :80.

### Velora frontend (`velora-frontend/`)

```bash
npm ci
npm run dev
npm run build
npm run lint
```

Env: `VITE_API_URL` (optional; empty → same-origin `/api/v1`). Optional: `VITE_ENABLE_MESSAGE_LENGTH_LIMIT=true`.

### Admin panel (`admin-panel/`)

```bash
npm install
npm run dev
npm run build
npm run lint
```

Env: `NEXT_PUBLIC_API_URL` (see `.env.local.example`, typically `http://localhost:5002`).

### Chat AI module (`chat-ai-module/`)

```bash
python -m venv .venv
# Windows: .venv\Scripts\activate
pip install -r requirements.txt
uvicorn main:app --reload --host 0.0.0.0 --port 8000
pytest tests/ -v
```

Backend calls this via `AiService:BaseUrl` (e.g. `http://localhost:8000`). Widget RAG needs `DATABASE_URL` pointing at the **same Postgres** as the .NET app.

### CI

`.github/workflows/deploy.yml` on `main`: restore/build/test backend, build frontend, then SSH deploy + `docker compose up -d --build`. Backend tests use `continue-on-error: true`.

## Coding conventions

### Backend

- New write/read use cases: `XxxCommand` / `XxxQuery` + handler + **FluentValidation validator** (pipeline throws if validator missing).
- Prefer handlers under `Core.Application/Handlers/` (some older query handlers live under `Queries/` — match nearby feature style).
- Admin features: `Core.Application/Administration/` + `WebAPI/Controllers/Admin/` + policy `Admin`.
- Controllers stay thin: map HTTP → `IMediator.Send`.
- Auth schemes: default `Bearer` (end users); `SupportUser` for support portal.
- Policies include `Admin`, `CompanyAdmin`, `SupportManager`, `SupportAgent`.
- Do not introduce a new Result monad; existing code returns DTOs or throws (handled by `GlobalExceptionMiddleware`).
- EF migrations live in `src/Infrastructure/Migrations/`. Runtime DB is **PostgreSQL (Npgsql)** despite older README mentions of SQL Server.
- Ignore orphan `src/Web/` (empty, not in solution) unless asked to remove it.

### Velora frontend

- State: Zustand stores in `src/store/use*Store.ts`.
- HTTP: axios in `src/services/api.ts` with Bearer + refresh; services as object exports.
- SignalR connections live in stores (`chatHub`, `callHub`) with `accessTokenFactory`.
- No `@/` path alias — use relative imports.
- Tailwind **v4** (`@import "tailwindcss"` in `src/index.css`).

### Admin panel

- App Router under `src/app/`; client pages use `'use client'`.
- HTTP: `fetch` helpers in `src/lib/api.ts` (not axios).
- Alias `@/*` → `./src/*`.
- Tailwind **v3**. Company vs support auth are separate stores/tokens.
- Some plan/limit UI still uses `MOCK_PLANS` — do not assume everything is mock; company/support APIs are real.

### Python AI module

- Keep FastAPI routers thin; agent tools under `agent/`; widget RAG under `api/`.
- Default LLM is Ollama; do not require OpenAI unless config switches providers.

## Testing rules

- Prefer adding/updating tests in `backend-core/tests/Tests/` (unit + integration) and/or `backend-core/src/Tests/UnitTests/`.
- Use xUnit + Moq; FluentAssertions appears in `src/Tests/UnitTests`.
- Integration-style tests often use EF InMemory — do not assume a live Postgres for unit tests.
- Frontend: no dedicated test runner scripts in `package.json` today — do not invent Jest/Vitest setup unless requested.
- Python: `pytest tests/ -v` from `chat-ai-module/`.

## Security rules

- Never commit secrets, tokens, connection strings with passwords, VAPID private keys, or JWT signing keys.
- Do not copy values from `.env`, `.env.local`, `appsettings.*.json` secrets, or `.continue` agent configs into docs or commits.
- Config key names only in docs (e.g. `ConnectionStrings:DefaultConnection`, `Jwt:Key`, `AiService:ApiKey`, `POSTGRES_PASSWORD`, `DATABASE_URL`, `VITE_API_URL`, `NEXT_PUBLIC_API_URL`).
- Authz: respect existing policies/schemes; guest/widget hubs are anonymous by design — do not weaken authenticated hubs.
- Sanitize logs: avoid passwords, tokens, message bodies with PII when adding logging.
- Treat `publish/` and `publish-chat/` as deployment outputs, not source of truth.

## Workflow expectations for agents

1. Read nearby code and existing patterns before changing architecture.
2. Keep diffs small and scoped to the request; do not refactor unrelated layers.
3. Preserve user local changes; do not revert unrelated dirty files.
4. Prefer matching existing naming, folder layout, and response envelopes.
5. After backend API contract changes, update the correct frontend consumer (`velora-frontend` vs `admin-panel`) and SignalR event names if needed.
6. For schema changes: add EF migration; do not hand-edit snapshot unless you know why.
7. Do not invent npm/`dotnet` scripts that are not in the repo.
8. Do not commit unless the user explicitly asks.
9. Avoid editing `publish/`, `publish-chat/`, and unrelated plans under `backend-core/.cursor/plans/` unless asked.

## Uncertainty / known drift

- Root and package READMEs historically lagged (e.g. .NET 8 / SQL Server). Prefer this file + `docs/` + code over outdated prose.
- `Jwt:*` must be supplied via environment / user-secrets (startup fails without a strong `Jwt:Key`). Do not rely on code defaults.
- Admin-panel production deploy (Docker/Liara) is **not** defined in-repo.
- Branding spelling: folder `velora-frontend` vs PWA name `Volera` — do not “fix” branding unless asked.
- Deploy details: `docs/DEPLOY-SERVER.md`.

## Deeper docs

- `docs/architecture.md`
- `docs/development.md`
- `docs/testing.md`
- `docs/security.md`
- `docs/logging.md` — Serilog → Seq structured event conventions
- `docs/credential-rotation.md`
- `docs/minio-storage.md`
- `docs/message-state-machine.md`
- `docs/adr-mongodb-unused.md`
- `docs/release-readiness-p0-p1.md`
- `docs/api.md`
- `docs/resilience-international-shutdown.md` — domestic-network / offline-first messaging
- Cursor rules: `.cursor/rules/*.mdc`
- Copilot: `.github/copilot-instructions.md`
- Maintain/update these files via skill: `.cursor/skills/maintain-agent-guidance/`
