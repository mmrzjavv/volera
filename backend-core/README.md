# Voice Call / Chat App Backend

.NET **9** backend for the chat and voice-call platform using Clean Architecture, DDD-inspired layers, CQRS (MediatR), and SignalR.

> Older copies of this README mentioned .NET 8 and SQL Server. The solution targets **`net9.0`** and uses **PostgreSQL (Npgsql)** at runtime.

## Architecture

- Clean Architecture / onion layering
- CQRS with MediatR + FluentValidation pipeline
- Real-time hubs (chat, calls, guest, company widget, support, AI widget)
- Modular monolith (Hangfire jobs, optional Redis/Mongo)

## Tech stack

- .NET 9 / ASP.NET Core Web API
- Entity Framework Core + Npgsql (PostgreSQL)
- SignalR, MediatR, FluentValidation, AutoMapper
- JWT authentication (end-user + SupportUser schemes)
- BCrypt password hashing
- Hangfire (PostgreSQL storage)
- xUnit tests

## Project structure

```
src/
├── Core.Domain/          # Entities, domain interfaces, events
├── Core.Application/     # Commands, Queries, Handlers, Validators, DTOs
├── Infrastructure/       # EF Core, migrations, repositories, security
├── WebAPI/               # Controllers, hubs, middleware
└── Shared/               # BaseEntity and shared primitives

tests/
└── Tests/                # Unit and integration tests

src/Tests/UnitTests/      # Additional unit tests (in solution)
```

## Getting started

### Prerequisites

- .NET 9 SDK
- PostgreSQL (or use Docker Compose)

### Local run

```bash
dotnet restore VoiceCallApp.sln
# Configure ConnectionStrings:DefaultConnection (appsettings.Development.json / user secrets)
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
dotnet run --project src/WebAPI
```

Dev URL: **http://localhost:5002** (see `src/WebAPI/Properties/launchSettings.json`).

### Docker Compose

```bash
cp .env.example .env   # set POSTGRES_PASSWORD
docker compose --env-file .env up -d --build
```

Runs API (:5000), Postgres, and builds `../velora-frontend` on port 80.

## API and hubs

- HTTP prefix: `/api/v1/...`
- Envelope: `ApiResponse<T>` (`success`, `data`, `message`, `operationDate`)
- Hubs: `/callHub`, `/chatHub`, `/guestHub`, `/companyWidgetHub`, `/supportHub`, `/aiWidgetHub`
- Ops: `/health`, `/version`, `/hangfire`

See repo root `docs/api.md` and controllers under `src/WebAPI/Controllers/`.

## Testing

```bash
dotnet test VoiceCallApp.sln
```

## Agent notes

Follow root [`AGENTS.md`](../AGENTS.md) and `.cursor/rules/backend.mdc` when changing this project.
