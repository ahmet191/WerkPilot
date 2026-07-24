# Configuration & Secrets

WerkPilot keeps **no real secrets in the repository**. Every sensitive value is
supplied at runtime through configuration providers, in this precedence order
(later wins): `appsettings.json` → `appsettings.{Environment}.json` → environment
variables → (Development only) .NET user-secrets.

The committed `appsettings*.json` files carry **empty strings or placeholders**
(e.g. `Gemini:ApiKey = ""`, `[GCP_API_KEY]`) — never a live value.

## Secrets the app reads

| Setting | Purpose | Local dev | Production |
|---|---|---|---|
| `ConnectionStrings:DefaultConnection` | PostgreSQL | `appsettings.Development.json` | `ConnectionStrings__DefaultConnection` env var |
| `Gemini:ApiKey` | AI ticket triage (Google Gemini) | `dotnet user-secrets set "Gemini:ApiKey" "<key>"` | `Gemini__ApiKey` env var |
| `Telegram:BotToken` / `Telegram:WebhookSecret` | Telegram notifications (optional) | user-secrets | `Telegram__BotToken` env var |
| `Storage:*` (S3/MinIO) | Attachment blob store | user-secrets / compose | env vars |
| SMTP / SSO / LDAP | email + optional auth | user-secrets | env vars |

Double-underscore (`__`) is the env-var form of a nested key
(`Gemini:ApiKey` → `Gemini__ApiKey`), portable across Linux/Docker.

## Local development

```bash
cd src/OmniFlow.Api
dotnet user-secrets set "Gemini:ApiKey" "<your-google-ai-studio-key>"
```

User-secrets live outside the repo (`~/.microsoft/usersecrets/...`) and are never
committed. If `Gemini:ApiKey` is unset the AI triage service degrades gracefully to
a labelled mock prediction — the app still runs.

## Production (Docker / Oracle Cloud, cloud-agnostic)

Provide values as environment variables on the container (compose `environment:`,
an env file, or the platform's secret store). Nothing vendor-specific is required.

```yaml
environment:
  - ConnectionStrings__DefaultConnection=Host=...;Database=OmniFlowDb;Username=...;Password=...
  - Gemini__ApiKey=${GEMINI_API_KEY}
```

## If a secret is ever committed

1. **Rotate/revoke it first** at the source (Google AI Studio for Gemini,
   BotFather for Telegram) — assume anything that reached git history is public.
2. Purge it from history (`git filter-repo`) and force-push; collaborators must
   re-clone (all commit hashes change).
3. Confirm with `gitleaks dir .` and `git log -S "<fragment>" --all`.
