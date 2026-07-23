# Chat Widget Admin Panel

Next.js 14 (App Router) SaaS admin for companies using the chat / AI widget, plus a support-agent portal.

> Early versions were mock-only. The app now calls the real backend for company, support, and AI-widget APIs (some plan-limit UI still uses `MOCK_PLANS`).

## Tech stack

- Next.js 14 (App Router), React 18, TypeScript
- Tailwind CSS v3, Zustand, Lucide React
- SignalR (`supportHub`, `aiWidgetHub`)
- `fetch`-based API helpers (not axios)

## Run

```bash
cp .env.local.example .env.local
# NEXT_PUBLIC_API_URL=http://localhost:5002
npm install
npm run dev
```

`npm run dev` finds a free port (3000+) and opens the browser (`scripts/dev-with-open.js`).

Other scripts: `npm run build`, `npm run start`, `npm run lint`.

## Environment

| Variable | Purpose |
|----------|---------|
| `NEXT_PUBLIC_API_URL` | Backend origin, no trailing slash (e.g. `http://localhost:5002`) |

## Features

- Landing, registration, dashboard (branches, support users, usage, widget generator, AI widget)
- Company session via `X-Company-Token` (`widget_admin_company` in localStorage)
- Support login / conversations with Bearer `SupportUser` JWT
- Role helpers in `src/lib/roles.ts` (demo role switcher still present)

## Folder structure

```
src/
  app/                 # App Router (landing, register, dashboard/*, support/*)
  components/          # ui/, landing/, registration/
  api/                 # company, support, aiWidget modules
  lib/                 # api.ts, cn.ts, roles.ts
  store/               # auth, support auth, company, widget
  hooks/               # useSupportHub, useAiWidgetHub
  data/                # MOCK_PLANS and related
  types/
```

Path alias: `@/*` → `./src/*`.

## Deploy

No in-repo Dockerfile or `liara.json` for this app yet. Run against a reachable API via `NEXT_PUBLIC_API_URL`.

## Agent notes

Do not confuse with platform admin inside `velora-frontend` (`/admin`). See root `AGENTS.md`.
