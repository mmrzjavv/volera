# Backend

ASP.NET Core API for Volera: REST (`/api/v1`), SignalR, EF Core, Hangfire.

```
src/
  Core.Domain/        entities, events
  Core.Application/   commands, queries, handlers
  Infrastructure/     EF Core, migrations, security
  WebAPI/             controllers, hubs, middleware
  Shared/
```

## Run

```bash
dotnet restore VoiceCallApp.sln
dotnet ef database update --project src/Infrastructure --startup-project src/WebAPI
dotnet run --project src/WebAPI
```

http://localhost:5002 — `GET /health`, `GET /version`

Configure `ConnectionStrings:DefaultConnection` and `Jwt:Key`.

## Docker

```bash
cp .env.example .env
docker compose --env-file .env up -d --build
```

API on :5000, Postgres, frontend on :80.

## Hubs

`/callHub` `/chatHub` `/guestHub` `/companyWidgetHub` `/supportHub` `/aiWidgetHub`

JWT hubs accept `access_token` on the query string.

## Test

```bash
dotnet test VoiceCallApp.sln
```
