# Testing

## Backend (.NET)

**Solution:** `backend-core/VoiceCallApp.sln`

**Projects:**

| Project | Path | Notes |
|---------|------|-------|
| Tests | `backend-core/tests/Tests` | Unit + Integration folders; xUnit, Moq, EF InMemory |
| UnitTests | `backend-core/src/Tests/UnitTests` | xUnit, Moq, FluentAssertions |

```bash
cd backend-core
dotnet test VoiceCallApp.sln
# or
dotnet test VoiceCallApp.sln -c Release
```

CI (`.github/workflows/deploy.yml`) runs backend tests with `continue-on-error: true` — still treat failures in code you own as blockers.

### What to test

- Command/query handlers (mocked repositories / unit of work)
- Repository behavior against InMemory DbContext where existing tests do
- Authz-sensitive paths when changing policies or admin endpoints

### What not to assume

- Live PostgreSQL, Redis, Mongo, or SignalR hosts for default unit tests
- A single unified test project — both test projects are valid

## Python AI module

```bash
cd chat-ai-module
pytest tests/ -v
```

`pyproject.toml` sets `testpaths = ["tests"]` and `asyncio_mode = auto`.

## Frontends

`velora-frontend` and `admin-panel` currently define:

- `npm run lint`
- `npm run build` (typecheck/build)

There is no committed Vitest/Jest/Playwright script. Prefer lint + build for regression checks unless/until a test runner is added.

## Manual smoke checks (local)

1. `GET http://localhost:5002/health` → OK
2. Register/login on Velora; open chat; confirm SignalR connects
3. Admin panel login against `NEXT_PUBLIC_API_URL`
4. If AI enabled: `GET http://localhost:8000/health` and a sample `/chat` or widget flow
