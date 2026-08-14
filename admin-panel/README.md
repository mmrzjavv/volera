# Company admin

Next.js app for widget companies and support agents. Not the platform admin at `/admin` on the PWA.

```bash
cp .env.local.example .env.local
npm install
npm run dev
```

| Env | |
|-----|--|
| `NEXT_PUBLIC_API_URL` | API origin, no trailing slash (`http://localhost:5002`) |

Company APIs use `X-Company-Token`. Support uses a SupportUser Bearer token and `/supportHub`.
