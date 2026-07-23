# Volera P0/P1 release readiness

Date: 2026-07-23  
Scope: P0 security + P1 messaging reliability / MinIO / UI polish (not full Telegram parity)

## Product identity

- Name: **Volera**
- Original teal/slate tokens in `velora-frontend/src/index.css`
- No Telegram trademarks, logos, or E2EE misclaims for chat

## Stack (detected)

- Backend: .NET 9, MediatR, SignalR, EF Core, PostgreSQL, Hangfire
- Frontend: React 19 + Vite PWA (`velora-frontend`)
- Admin SaaS: Next.js (`admin-panel`)
- AI: FastAPI (`chat-ai-module`)
- Media: MinIO/S3 via `LiaraStorageService`
- Realtime: SignalR hubs

## P0 completed

- [x] Sanitized committed appsettings (placeholders only)
- [x] Expanded `.env.example` + root `.gitignore`
- [x] JWT fail-fast (no hardcoded signing keys)
- [x] Company demo OTP gated (`Auth:AllowDemoCompanyOtp` + Development only)
- [x] Hangfire dashboard restricted
- [x] Call UI relabeled (DTLS-SRTP, not E2EE messaging)
- [x] Credential rotation doc (paths only)

## P1 completed

- [x] Outbox for edit / delete / reactions
- [x] Rate limits: auth, message send, uploads
- [x] Session list + revoke in Profile security
- [x] MinIO private uploads + MIME validation + ready health
- [x] Volera tokens, empty state, locale/RTL chrome (en/fa)
- [x] Unit tests for media validation, company OTP, message outbox handlers

## Explicitly not done (roadmap)

Channels, forum topics, granular group permissions, polls, folders, stories, bots, true E2EE, full E2E/chaos/load suite, 2FA, email verification

## Operator actions before production

1. Rotate all compromised credentials ([credential-rotation.md](./credential-rotation.md))
2. Set strong `JWT_SECRET` in deploy env
3. Private MinIO bucket + backup policy
4. Confirm `/health/ready` returns Ready
5. Do not enable `Auth:AllowDemoCompanyOtp` in production

## Recommendation

**Conditional go** for internal/staging after secret rotation and smoke login/send/sync.  
**Not** Telegram-parity production-ready; remaining P2/P3 gaps are intentional.
