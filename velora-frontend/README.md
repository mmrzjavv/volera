# Volera PWA

End-user chat and calls. Platform admin lives at `/admin`.

```bash
npm ci
npm run dev
```

Vite proxies `/api` and hubs to `http://localhost:5002`.

| Env | |
|-----|--|
| `VITE_API_URL` | API origin. Empty = same-origin `/api/v1` |
| `VITE_ENABLE_MESSAGE_LENGTH_LIMIT` | `true` to cap message length |

```
npm run build
npm run lint
npm run preview
```

Calls need HTTPS or localhost. See [docs/development.md](../docs/development.md).
