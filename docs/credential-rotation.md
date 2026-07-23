# Credential rotation and secret hygiene

## Status

Previously exposed development credentials (chat history, committed `appsettings*.json`, local Docker env files, and historical `publish/**` artifacts) must be treated as **compromised**.

This document lists **locations only**. Do not paste secret values into chat, commits, tickets, or screenshots.

## Findings (paths)

| Area | Paths |
|------|--------|
| Backend config (sanitized to placeholders in source) | `backend-core/src/WebAPI/appsettings.json`, `appsettings.Development.json` |
| Local-only (gitignored) | `backend-core/.env.docker`, `backend-core/src/WebAPI/appsettings.DockerLocal.json` |
| Frontend local env | `velora-frontend/.env*`, `admin-panel/.env.local` |
| Agent config risk | `.continue/agents/*.yaml` |
| Deployment artifacts (do not use as source of truth) | `publish/**`, `publish-chat/**` |
| Hardcoded JWT fallbacks (removed) | `JwtTokenGenerator`, `SupportUserJwtTokenGenerator`, `DependencyInjection` auth wiring |

## Rotation checklist

1. **PostgreSQL / Redis / Mongo / MinIO**: change passwords and access keys on the host; update only gitignored env files.
2. **JWT**: generate new `Jwt__Key` / `JWT_SECRET` (≥32 chars, unique). Restart API so old tokens fail validation.
3. **VAPID**: regenerate push key pair; update clients.
4. **Company / support tokens**: revoke sessions; re-issue company tokens.
5. **Cloud Liara / third-party**: rotate any keys that ever appeared in committed appsettings or publish folders.
6. **Do not** reuse any password previously shared in chat.

## Git history cleanup (manual authorization required)

Do **not** rewrite shared history without explicit owner approval.

If secrets exist in history:

1. Inventory with `gitleaks` / `git log --all --full-history -- <path>` (report paths only).
2. Rotate all affected credentials first (history rewrite does not undo leaks).
3. Optionally use `git filter-repo` or BFG on a coordinated maintenance window.
4. Force-push only with team agreement; invalidate all clones afterward.

## Runtime rules

- Prefer environment variables / user-secrets over committed JSON.
- Copy [`backend-core/.env.example`](../backend-core/.env.example) → `.env` / `.env.docker` and fill placeholders.
- `Auth:AllowDemoCompanyOtp` must remain `false` outside Development.
- Hangfire dashboard is open in Development only; production requires authenticated Admin/Moderator/SuperAdmin.
