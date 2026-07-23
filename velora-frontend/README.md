# Velora frontend

React + TypeScript + Vite PWA for the chat / voice-call end-user app. Also hosts the **platform** admin UI under `/admin/*`.

Package name in `package.json` is `frontend`; PWA manifest may show `Volera`.

## Stack

- React 19, Vite 7, TypeScript
- Zustand, React Router 7, Axios, SignalR
- Tailwind CSS v4, Lucide icons
- `vite-plugin-pwa`

## Scripts

```bash
npm ci
npm run dev       # Vite dev server (proxies API/hubs → localhost:5002)
npm run build     # tsc -b && vite build
npm run lint
npm run preview
```

## Environment

| Variable | Purpose |
|----------|---------|
| `VITE_API_URL` | API origin (optional). Empty → same-origin `/api/v1`. Trailing `/api/v1` is normalized. |
| `VITE_ENABLE_MESSAGE_LENGTH_LIMIT` | Set to `true` to enable client message length limit behavior |

## Source layout

```
src/
  pages/           # Login, Register, Chat, Profile, Invite, AdminMessages, admin/*
  components/      # chat/, admin/, ui/
  store/           # useAuthStore, useChatStore, useCallStore, …
  services/        # api.ts, adminApi.ts, call/contact/groupCall services
  hooks/
  types/
  utils/
```

## Backend integration

- REST: axios instance in `src/services/api.ts` → `/api/v1` with Bearer + refresh
- SignalR: `chatHub` / `callHub` from Zustand stores (`accessTokenFactory`)
- Local storage keys: `token`, `refreshToken`, `user`

## Docker

`Dockerfile` builds with Node 22 and serves via nginx. `nginx.conf` proxies `/api/` and SignalR hubs to `webapi:8080`. Used from `backend-core/docker-compose.yml`.

## Agent notes

This is **not** the company widget admin (`../admin-panel`). See root `AGENTS.md` and `.cursor/rules/frontend.mdc`.
