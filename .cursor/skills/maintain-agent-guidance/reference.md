# Agent guidance sync matrix

Use when auditing or updating files so each concern lives in the right place.

## Source of truth order

1. **Code + tooling** (csproj, package.json, compose, Program.cs, workflows)
2. **`AGENTS.md`** (canonical for agents)
3. **`.cursor/rules/*.mdc`** (short, scoped enforcement)
4. **`docs/*`** (deeper human/agent reference)
5. **Package READMEs** / root `README.md` (onboarding)
6. **`.github/copilot-instructions.md`** (short Copilot mirror of AGENTS)

If code disagrees with docs, fix docs (or flag intentional exceptions).

## What goes where

| Concern | Primary | Also update if changed |
|---------|---------|------------------------|
| Monorepo map / product roles | `AGENTS.md` | root `README.md`, `docs/architecture.md` |
| Backend layering / CQRS / auth | `AGENTS.md`, `.cursor/rules/backend.mdc` | `docs/architecture.md`, `docs/api.md`, `backend-core/README.md` |
| DB / migrations / Postgres | `.cursor/rules/database.mdc` | `AGENTS.md`, `docs/development.md`, `docs/security.md` |
| Frontend conventions | `.cursor/rules/frontend.mdc` | package READMEs, `AGENTS.md` |
| AI module | `.cursor/rules/chat-ai-module.mdc` | `chat-ai-module/README.md`, `docs/api.md` |
| Tests | `.cursor/rules/testing.mdc`, `docs/testing.md` | `AGENTS.md` |
| Security / secrets | `.cursor/rules/security.mdc`, `docs/security.md` | always-apply project rules |
| Verified commands / ports | `AGENTS.md`, `docs/development.md` | root + package READMEs, Copilot file |
| Deploy | `docs/DEPLOY-SERVER.md` | root README link only unless flow changes |
| Workflow / “don’t invent scripts” | `AGENTS.md`, `.cursor/rules/project-architecture.mdc` | Copilot file |

## Current Cursor rules set

Expected under `.cursor/rules/`:

- `project-architecture.mdc` — alwaysApply
- `security.mdc` — alwaysApply
- `backend.mdc` — `backend-core/**/*.{cs,csproj}`
- `frontend.mdc` — velora + admin-panel TS/CSS
- `database.mdc` — Infrastructure
- `testing.mdc` — test trees
- `chat-ai-module.mdc` — Python AI module

Add a new `.mdc` only for a durable, distinct concern; prefer editing an existing rule.

## Drift checklist (quick audit)

Run mentally or via search:

- [ ] README claims match TFM (`net9.0`) and DB (PostgreSQL)
- [ ] `dotnet` / `npm` / `pytest` / `uvicorn` / `docker compose` commands still exist
- [ ] Hub paths and auth schemes still match `Program.cs` / hub attributes
- [ ] Env var **names** in docs match `.env*.example` (no values)
- [ ] Velora vs admin-panel roles not confused
- [ ] No secrets in examples, Continue configs, or docs
- [ ] Copilot instructions still a short accurate subset of AGENTS

## After large feature work

If a change affects agents’ default behavior (new app folder, new auth scheme, new required command, new hub):

1. Update `AGENTS.md` first
2. Patch the matching `.mdc`
3. Patch `docs/*` sections that would otherwise lie
4. Touch Copilot instructions if the one-screen summary changed
5. Fix package README only if setup steps changed
