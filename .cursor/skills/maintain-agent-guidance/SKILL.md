---
name: maintain-agent-guidance
description: >-
  Create, audit, and update AI-agent guidance files (AGENTS.md, .cursor/rules,
  .github/copilot-instructions.md, package READMEs, docs/architecture|development|testing|security|api).
  Use when the user asks to maintain agent docs, refresh AGENTS.md, sync Cursor rules,
  update Copilot instructions, fix stale setup commands, or keep agent guidance aligned
  after architecture, command, auth, or stack changes.
---

# Maintain agent guidance

Keep AI-agent instruction files accurate for this monorepo. Prefer small merges over rewrites.

## Inventory (this repo)

| File | Role |
|------|------|
| `AGENTS.md` | Canonical agent instructions |
| `.cursor/rules/*.mdc` | Cursor rules (split by concern) |
| `.github/copilot-instructions.md` | Copilot project instructions |
| `README.md` + package READMEs | Human setup; must not contradict AGENTS |
| `docs/architecture.md` | System map |
| `docs/development.md` | Local run / ports |
| `docs/testing.md` | Test locations and commands |
| `docs/security.md` | Secrets and authz |
| `docs/api.md` | API/hub overview |
| `docs/DEPLOY-SERVER.md` | Deploy (update only if deploy facts change) |
| `.cursor/skills/maintain-agent-guidance/` | This skill |

Optional: `CLAUDE.md`, `.cursorrules` — only if present; do not invent unless asked.

Detailed sync matrix: [reference.md](reference.md).

## When to run

- User asks to create/update/audit agent guidance, AGENTS.md, Cursor rules, or Copilot instructions
- Stack, ports, auth, folder layout, or verified commands changed
- READMEs or docs drift from code (e.g. wrong TFM, wrong DB)

## Workflow

Copy and track:

```
Guidance maintenance:
- [ ] 1. Discover existing agent files (do not overwrite blindly)
- [ ] 2. Re-verify facts from code/tooling (not from memory)
- [ ] 3. Decide create vs merge vs leave alone
- [ ] 4. Apply minimal edits; keep concerns split
- [ ] 5. Cross-check consistency + scrub secrets
- [ ] 6. Summarize changes + remaining unknowns
```

### 1. Discover first

Read before writing:

- Root: `AGENTS.md`, `README.md`, `CLAUDE.md`, `.cursorrules`
- `.cursor/rules/*.mdc`, `.cursor/skills/**/SKILL.md`
- `.github/copilot-instructions.md`, `.github/workflows/*`
- Package READMEs under `backend-core/`, `velora-frontend/`, `admin-panel/`, `chat-ai-module/`
- `docs/*.md`

Preserve useful project-specific rules. Merge carefully; never blank valuable content.

### 2. Re-verify from the repo

Do **not** invent commands or stack claims. Confirm from:

| Fact | Sources |
|------|---------|
| .NET TFM / projects | `*.csproj`, `VoiceCallApp.sln` |
| Frontend scripts / deps | `package.json` |
| Python run/tests | `chat-ai-module/README.md`, `requirements.txt`, `pyproject.toml` |
| Compose / ports | `docker-compose.yml`, `launchSettings.json`, Vite/Next config |
| CI | `.github/workflows/*.yml` |
| Auth / hubs / routes | `Program.cs`, hubs, controllers |
| Env **names** only | `.env.example`, `.env.local.example`, appsettings **keys** |

If uncertain, write “Uncertainty” / flag for the user — do not guess.

### 3. Edit rules

- **AGENTS.md** stays the single source of truth for agents; other files should agree with it.
- Cursor rules: one concern per `.mdc`; correct frontmatter (`description`, `globs` or `alwaysApply`).
- Keep rules concise and actionable; link to `docs/*` for depth.
- Update package READMEs only for missing/wrong setup facts — no full rewrites unless poor/missing.
- Do not edit `publish/`, `publish-chat/`, or plan dumps under `backend-core/.cursor/plans/` unless asked.
- Do not commit unless the user asks.

### 4. Security scrub (required)

Before finishing:

- No passwords, API keys, JWT/VAPID private keys, or full credentialed connection strings
- Examples use placeholders (`CHANGE_ME`, `YOUR_…_KEY`)
- Document config **key names** only
- Do not paste values from `.env`, `.env.local`, `.continue/agents/*`, or secret appsettings fields into docs

### 5. Consistency check

After edits, ensure these match across AGENTS / rules / docs / READMEs:

- App roles: Velora = end-user + platform `/admin`; `admin-panel` = company/support
- Backend: `net9.0`, PostgreSQL/Npgsql, `api/v1`, MediatR + FluentValidation required
- Dev API port **5002**; Compose API **5000**
- Commands only if present in package files / solution / documented scripts

### 6. Output to user

Always report:

1. Files created/updated (and left alone)
2. Facts corrected (e.g. stack/port/command drift)
3. Remaining unknowns the user should confirm
4. Commands/paths used to verify

## Cursor rule format reminder

```markdown
---
description: Short picker description
globs: backend-core/**/*.{cs,csproj}
alwaysApply: false
---

# Title
…
```

Always-on rules use `alwaysApply: true` and omit or ignore globs as appropriate.

## Anti-patterns

- Rewriting all guidance when one command changed
- Copying secrets “for completeness”
- Inventing npm/dotnet/pytest scripts not in the repo
- Mixing platform admin and company admin guidance
- Treating `publish*` as source of truth
