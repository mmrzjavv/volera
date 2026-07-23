# Architecture

## System map

```
                    ┌─────────────────────┐
                    │  velora-frontend    │
                    │  React/Vite PWA     │
                    │  + platform /admin  │
                    └─────────┬───────────┘
                              │ HTTP /api/v1 + SignalR
                              │ (chatHub, callHub, …)
┌─────────────────────┐       │
│  admin-panel        │───────┤
│  Next.js company /  │       │
│  support SaaS       │       ▼
└─────────────────────┘  ┌────────────────────┐
                         │  backend-core      │
                         │  ASP.NET Core 9    │
                         │  WebAPI + Hubs     │
                         └─────────┬──────────┘
                    ┌──────────────┼──────────────┐
                    ▼              ▼              ▼
              PostgreSQL      Redis/Mongo     chat-ai-module
              (required)      (optional)      FastAPI :8000
                                              (AiService)
```

Docker Compose (`backend-core/docker-compose.yml`) runs **webapi**, **postgres**, and **velora-frontend** (nginx proxy). It does not currently compose Redis, Mongo, admin-panel, or chat-ai-module.

## Backend layers

| Project | Responsibility |
|---------|----------------|
| `Shared` | `BaseEntity` and shared primitives |
| `Core.Domain` | Entities, domain interfaces, events |
| `Core.Application` | CQRS (MediatR), validators, DTOs, admin use cases |
| `Infrastructure` | EF Core, migrations, repositories, security, external services |
| `WebAPI` | Controllers, hubs, middleware, composition root |

Dependency direction: Domain ← Application ← Infrastructure/WebAPI.

## Real-time and reliable delivery

SignalR hubs (see `WebAPI/Program.cs`): `/callHub`, `/chatHub`, `/guestHub`, `/companyWidgetHub`, `/supportHub`, `/aiWidgetHub`.

Velora outbound messages use an IndexedDB queue + `clientMessageId` idempotency. Server writes a transactional **outbox** with the message and a background processor delivers SignalR/push. HTTP `POST /api/v1/Message` and `GET /api/v1/Message/sync` support offline retry and gap recovery. See [resilience-international-shutdown.md](resilience-international-shutdown.md).

## Auth model

| Audience | Mechanism |
|----------|-----------|
| End users (Velora) | JWT Bearer + refresh tokens |
| Platform admins | Same user JWT + `Admin` policy (roles Admin/Moderator/SuperAdmin) |
| Company admins | Company token header `X-Company-Token` |
| Support agents | JWT scheme `SupportUser` |
| Guest / public widgets | Anonymous hubs + dedicated guest/widget token flows |

## Data stores

- **PostgreSQL**: primary app data + Hangfire; `ApplicationDbContext`
- **Redis**: optional (presence/queues — config keys present)
- **MongoDB**: removed from error logging; use **Serilog → Seq** (`SEQ_URL`)
- **Object storage**: `Storage:*` (S3-compatible / Liara)

## Frontends

- **Velora**: SPA state in Zustand; axios API client; hubs in stores.
- **Admin panel**: App Router; fetch API client; separate company vs support sessions.

## AI widget path

1. Company configures AI widget via admin-panel / company APIs.
2. Backend stores content blocks; may call AI service to embed (`/embed`).
3. Chat requests go to AI service `/chat` with `tenant_id` / session; RAG reads `AiContentBlocks` from shared Postgres.

## Out of scope / non-source

- `publish/`, `publish-chat/`: build artifacts
- `backend-core/src/Web/`: empty orphan tree (not in solution)
- `backend-core/.cursor/plans/`: historical plans, not runtime docs
