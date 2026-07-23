# API overview

Base path for HTTP APIs: **`/api/v1`**.

Response envelope (typical):

```json
{
  "success": true,
  "operationDate": "...",
  "data": {},
  "message": null
}
```

Exact DTO shapes live in `backend-core/src/Core.Application` (and WebAPI DTOs where used). Prefer OpenAPI/Swagger in Development over duplicating every field here.

## Auth

| Area | Route prefix (examples) | Auth |
|------|-------------------------|------|
| User auth | `api/v1/Auth` | Anonymous for login/register; Bearer after |
| Key exchange | `api/v1/auth` | See `KeyExchangeController` |
| User profile | `api/v1/User` | Bearer |
| Sessions | `api/v1/Session` | Bearer |
| Messages / groups / calls | `api/v1/Message`, `Group`, `Call`, `GroupCall` | Bearer |
| Uploads / push | `api/v1/Upload`, `Push` | Bearer |
| Guest | `api/v1/guest` | Guest/token flows + rate limits |
| Company | `api/v1/company`, `company/widget`, `company/ai-widget`, `company/support-users` | Company token / policies |
| Support | `api/v1/support`, `support/users` | SupportUser Bearer |
| Platform admin | `api/v1/admin/*` | `Admin` policy |
| System messages / errors | `api/v1/system-messages`, `errors` | Per-controller |

## SignalR hubs

| Path | Typical client |
|------|----------------|
| `/chatHub` | Velora chat |
| `/callHub` | Velora calls |
| `/guestHub` | Guest chat |
| `/companyWidgetHub` | Company widget |
| `/supportHub` | Admin-panel support |
| `/aiWidgetHub` | AI widget |

Pass JWT as query `access_token` when required.

## Ops endpoints

- `GET /health`
- `GET /version`
- Hangfire UI: `/hangfire`

## External AI service (`chat-ai-module`)

Not under `/api/v1`. Common routes:

| Method | Path | Role |
|--------|------|------|
| GET | `/health` | Health |
| POST | `/agent/chat` | Agent chat |
| POST | `/embed` | Widget embedding |
| POST | `/chat` | Widget chat + RAG |
| POST | `/embeddings/*` | Embedding CRUD/query |
| POST | `/text/to-speech`, `/speech/to-text` | Speech |

Backend integrates via `AiService:BaseUrl` (+ optional `AiService:ApiKey`).

## Client notes

- **Velora** axios base resolves to `{origin or VITE_API_URL}/api/v1`.
- **Admin panel** uses absolute `NEXT_PUBLIC_API_URL` + full `/api/v1/...` paths.
- Compose/nginx frontend proxies `/api/` and hub paths to `webapi:8080`.

For endpoint-level behavior, read the matching controller under `backend-core/src/WebAPI/Controllers/`.
