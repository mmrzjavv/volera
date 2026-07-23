# Security

## Secrets handling

**Do not commit** passwords, API keys, JWT signing keys, VAPID private keys, storage keys, or full connection strings with credentials.

Templates (names only — fill locally):

| File | Keys |
|------|------|
| `backend-core/.env.example` | `POSTGRES_PASSWORD` |
| `admin-panel/.env.local.example` | `NEXT_PUBLIC_API_URL` |
| `chat-ai-module/.env.example` | `LLM_PROVIDER`, `DATABASE_URL`, Ollama/OpenAI-related names |

Backend also uses configuration keys such as:

- `ConnectionStrings:DefaultConnection`, `ConnectionStrings:Redis`
- `SEQ_URL` / `Seq:ServerUrl`, `Seq:UiUrl` (Serilog → Seq)
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience` (+ `Jwt:SupportUser:*`)
- `AiService:BaseUrl`, `AiService:ApiKey`
- `Storage:AccessKey`, `Storage:SecretKey`, `Storage:BucketName`, `Storage:EndpointUrl`
- `VapidPublicKey` / `VapidPrivateKey` or `PushNotifications:*`

Prefer user secrets, environment variables, or server `.env` (gitignored). See [credential-rotation.md](./credential-rotation.md), [minio-storage.md](./minio-storage.md), and [release-readiness-p0-p1.md](./release-readiness-p0-p1.md).

**Runtime:** `Jwt:Key` is required at startup (min 32 characters; placeholders rejected). Company demo OTP requires Development **and** `Auth:AllowDemoCompanyOtp=true`.

**Known risk:** historical `publish/**` and old commits may still contain secrets — rotate and optionally purge history with explicit authorization.

## Authentication and authorization

- End-user API/hubs: JWT Bearer; refresh-token flow on 401 in Velora.
- Support portal: separate `SupportUser` JWT scheme and policies (`SupportManager`, `SupportAgent`).
- Company APIs: `X-Company-Token`.
- Platform admin: `Admin` policy (Admin/Moderator/SuperAdmin roles).
- Guest/widget hubs are anonymous by design — do not copy that pattern to authenticated hubs.

## API and data safety

- Validate all new MediatR requests with FluentValidation.
- Controllers should stay thin; business rules belong in handlers/domain.
- `GlobalExceptionMiddleware` maps validation and common exceptions to HTTP status codes — avoid leaking stack traces to clients in production.
- Do not log tokens, passwords, or unnecessary PII from chat content.
- Uploads go through configured storage — never embed cloud credentials in frontends.

## Deploy

GitHub Actions needs secrets: `SSH_PRIVATE_KEY`, `SERVER_HOST`, `SERVER_USER`, optional `SERVER_DEPLOY_PATH`.  
Server holds `backend-core/.env` with `POSTGRES_PASSWORD`. Details: [DEPLOY-SERVER.md](DEPLOY-SERVER.md).

## Agent/tooling configs

Do not store live API keys in `.continue/agents/*.yaml` or similar committed agent configs. Use environment variables or local-only overrides ignored by git.
