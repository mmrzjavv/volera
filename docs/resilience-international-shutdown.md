# International internet shutdown resilience

This document describes how Chat-App-DotNet behaves when **international** internet is blocked but a **domestic** path to shared messaging infrastructure remains available.

Honest constraint: users can only exchange messages when clients can reach a shared API, database, and (preferably) realtime gateway on a network they can still access. There is no P2P fallback in this codebase.

## Architecture (critical path)

| Component | Role in messaging | Must be domestic-reachable |
|-----------|-------------------|----------------------------|
| ASP.NET Core API + SignalR | Create/sync messages, realtime | Yes |
| PostgreSQL | Durable message + outbox store | Yes |
| Velora PWA | Offline-first outbound queue + sync | Client + domestic API |
| Object storage (S3/Liara/MinIO) | Attachments only | Prefer domestic; text works without it |
| Web Push (VAPID → browser vendors) | Background wake only | Often foreign; **not required** for open-app sync |
| Redis / Mongo | Presence / error logs / AI | Optional |
| Ollama / AI module | AI widget | Not on critical text path |

## Dependency inventory (runtime)

| Dependency | Critical for text? | Shutdown risk | Mitigation in repo |
|------------|--------------------|---------------|--------------------|
| Domestic API + SignalR + Postgres | Yes | Total outage if unreachable | Deploy on domestic host; see checklist below |
| Object storage (`Storage:*`) | No (text) | Previously could block API boot | Optional/null storage when unset |
| Web Push vendor endpoints | No | Background notify fails | Foreground SignalR + HTTP sync |
| jsDelivr fonts | No | Fonts fall back | Self-hosted Vazirmatn in Velora |
| Hardcoded Liara API URL (admin) | Admin only | Wrong foreign endpoint | Require `NEXT_PUBLIC_API_URL` |
| Docker Hub / npm / mcr.microsoft.com | Ops rebuild | Cannot rebuild images | Pre-pull images; domestic mirrors |
| Authoritative DNS / TLS | Yes | Clients cannot resolve/trust API | Domestic DNS; valid certs; no verify-disable |

## Message reliability (implemented)

1. **Client:** Velora persists outgoing messages (IndexedDB) with a stable `clientMessageId` **before** network send; retries with backoff; UI shows queued/sending/accepted/failed.
2. **Server:** Unique `(SenderId, ClientMessageId)` → idempotent create; `POST /api/v1/Message` + SignalR both accept `clientMessageId` and return server id.
3. **Outbox:** Message row + outbox row in one transaction; background processor delivers SignalR/push; retries then dead-letters.
4. **Sync:** `GET /api/v1/Message/sync` keyset recovery after reconnect / when SignalR is down.
5. **Attachments:** Text send continues if storage is unavailable; uploads return clear failure.

## Infrastructure checklist (ops — outside pure code)

- [ ] API, Postgres, and frontend reverse-proxy hosted on domestically reachable network
- [ ] Domestically reachable DNS for the API hostname (or IP-based access with documented TLS strategy)
- [ ] TLS certificates that validate without foreign OCSP soft-fail blocking clients (test under shutdown)
- [ ] Object storage: MinIO on same Compose network **or** domestic S3-compatible endpoint (not foreign-only)
- [ ] Pre-pulled container images before a crisis; configure domestic registry mirrors if rebuilds are required
- [ ] Do **not** rely on unsigned client-supplied endpoint lists for auth token destinations
- [ ] Web Push may fail; train ops/users that open-app sync is the guaranteed path

### Suggested Compose addition (MinIO — optional)

Run MinIO beside `webapi`/`postgres` and point `Storage:EndpointUrl` at the internal service URL. Keep credentials in server `.env` only.

## International-isolation test (local)

### Goal

Prove health, auth refresh, send (with `clientMessageId`), and sync work when foreign hostnames are unreachable but Compose services remain reachable.

### Procedure

1. Start stack from `backend-core`:

```bash
docker compose --env-file .env up -d --build
```

2. On the test host (or a sidecar container), block foreign destinations while allowing Compose bridge DNS (`webapi`, `postgres`, `frontend`). Example (Linux host; adjust iface):

```bash
# Illustrative — do not run blindly on a shared workstation
sudo iptables -A OUTPUT -d 127.0.0.0/8 -j ACCEPT
# allow docker bridge subnet (inspect: docker network inspect ...)
# then REJECT known foreign CDNs / push / package hosts used in docs
```

Or use the helper script: [`backend-core/scripts/shutdown-isolation-smoke.sh`](../backend-core/scripts/shutdown-isolation-smoke.sh) (hits only configured domestic base URL).

3. Smoke expectations:

- `GET {API}/health` → OK
- Login / refresh against domestic API
- `POST /api/v1/Message` with same `clientMessageId` twice → one DB row
- `GET /api/v1/Message/sync?...` returns the message after simulated SignalR gap
- App UI loads without jsDelivr

4. CI cannot fully emulate national firewalls; document results of the manual isolation run in the release checklist.

## Rollback

- EF migration `AddMessageClientIdAndOutbox`: roll back migration if needed; clients without queue still work if they omit `clientMessageId` (legacy SignalR).
- Feature flags are not required; disable outbox processor only if emergency (messages remain in DB; realtime delayed until processor resumes).

## Remaining limitations

- No messaging if domestic API/DB unreachable.
- Background delivery without Web Push vendor reachability is OS-limited for killed PWAs.
- Guest/widget/admin/AI offline parity is not fully covered by this pass.
- Certificate issuance during shutdown may fail if ACME depends on foreign infrastructure.
- **Calls (voice/video/screen share):** Self-hosted **Coturn** in Docker (`chat-coturn`) provides STUN+TURN. Clients load ICE from `GET /api/v1/Call/ice-servers` (no Google/Twilio). Set `TURN_PUBLIC_HOST` / `TURN_EXTERNAL_IP` to a host reachable by all callers; open UDP/TCP **3478** and UDP **40000–40050**. Same-LAN still works via host candidates when Coturn is unreachable.
