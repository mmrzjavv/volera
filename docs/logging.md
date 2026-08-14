# Logging conventions (Serilog → Seq)

## Goal

Production logs tell a **story of important business events**, not EF SQL dumps or handler chatter.

## Stack

- **Serilog** (`Serilog.AspNetCore` + `Serilog.Sinks.Seq`)
- **Seq** container on `chat-app-net`
  - From containers: `SEQ_URL=http://seq` (Seq listens on port 80 inside the network)
  - From host UI / `dotnet run`: `http://localhost:5341`
  - Config keys: `SEQ_URL` / `Seq:ServerUrl`, `Seq:UiUrl`
- Enrichers: environment, machine, thread, LogContext (`CorrelationId`, `TraceId`, `UserId`, `Username`, `SessionId`, `ClientIp`)

## Levels

| Scope | Production floor |
|-------|------------------|
| Application business events | `Information` |
| Expected auth/validation failures | `Warning` |
| Unexpected failures / jobs | `Error` |
| Framework (ASP.NET, EF Core, Hangfire, HttpClient) | `Warning`+ (EF Query compilation → `Error`) |
| Successful HTTP request logging | `Debug` (hidden in production) |
| MediatR command duration | `Debug` |

## Event naming

Use PascalCase event names via `Core.Application.Logging.AppLogEvents` + `AppLog.Info/Warning/Error`:

```csharp
AppLog.Info(_logger, AppLogEvents.UserLoginSucceeded,
    "UserId: {UserId} | Username: {Username} | IP: {ClientIp} | Method: Password | Result: Success",
    userId, username, ip);
```

Seq query examples:

- `EventName = 'UserLoginFailed'`
- `CorrelationId = '...'`
- `UserId = '...' and EventName like '%Call%'`

## Never log

Passwords, tokens, refresh tokens, cookies, session keys (hex), full request bodies, message content with PII, huge JSON payloads.

## Correlation

`RequestLogContextMiddleware` accepts/returns `X-Correlation-ID` and pushes `CorrelationId` + `TraceId` into Serilog LogContext (runs after authentication so user claims are available).
