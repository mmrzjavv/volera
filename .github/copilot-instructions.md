# GitHub Copilot instructions — Chat-App-DotNet

## Context

Monorepo for a chat/voice platform:

- `backend-core` — .NET 9 Clean Architecture API + SignalR + EF Core (PostgreSQL)
- `velora-frontend` — React 19 / Vite end-user PWA + platform admin
- `admin-panel` — Next.js 14 company widget / support admin
- `chat-ai-module` — FastAPI AI + RAG service

Follow `AGENTS.md` for full rules.

## Prefer

- Small, focused changes matching existing patterns
- Backend features as MediatR Command/Query + Handler + FluentValidation + thin `api/v1` controller
- `ApiResponse<T>` envelope on HTTP APIs
- Correct consumer app when changing contracts (`velora-frontend` vs `admin-panel`)
- Existing auth: Bearer (users), SupportUser scheme, company `X-Company-Token`

## Avoid

- Inventing commands/scripts not in the repo
- Mixing platform admin (`velora-frontend/src/pages/admin`) with company admin (`admin-panel`)
- Committing secrets or hardcoding credentials
- Editing `publish/` or `publish-chat/` unless asked
- Broad refactors unrelated to the task
- Assuming SQL Server; runtime DB is PostgreSQL (Npgsql)

## Commands

```bash
# Backend
cd backend-core && dotnet test VoiceCallApp.sln && dotnet run --project src/WebAPI

# Velora
cd velora-frontend && npm run lint && npm run build

# Admin panel
cd admin-panel && npm run lint && npm run build

# AI module
cd chat-ai-module && pytest tests/ -v
```

Dev API port: **5002**. Docker compose API port: **5000** (maps container 8080).
