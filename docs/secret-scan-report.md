# Secret scan report (paths only)

Scan date: 2026-07-23  
Method: manual path inventory + sanitization of committed appsettings. Values are not reproduced here.

## Remediated in source tree

| Path | Action |
|------|--------|
| `backend-core/src/WebAPI/appsettings.json` | Replaced secrets with empty placeholders |
| `backend-core/src/WebAPI/appsettings.Development.json` | Replaced secrets with empty placeholders |
| JWT hardcoded fallbacks in DI / token generators | Removed; startup requires `Jwt:Key` |
| Company demo OTP hardcoded accept | Gated behind Development + `Auth:AllowDemoCompanyOtp` |

## Still treat as compromised (rotate)

| Path / area | Notes |
|-------------|--------|
| Git history of former appsettings | Likely contains old cloud credentials |
| `publish/**`, `publish-chat/**` | Deployment copies of appsettings — do not use; rotate any keys that lived there |
| `backend-core/.env.docker` (gitignored) | Local Docker secrets — rotate on host |
| `appsettings.DockerLocal.json` (gitignored) | Local only |
| `.continue/agents/*.yaml` | May contain tooling secrets historically |
| Chat-supplied passwords | Compromised by disclosure |

## Recommended tools (operator)

```bash
# Install gitleaks separately, then:
gitleaks detect --source . --report-path gitleaks-report.json
```

Do not commit the report if it embeds secret values.
