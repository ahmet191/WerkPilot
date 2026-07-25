# WerkPilot — OmniFlow ESM

Multi-tenant Enterprise Service Management: an HRIS and a dynamic ITSM/ticketing
platform in one application. Ticket types, forms, workflows and interface text
are configured per tenant at runtime rather than compiled in.

Built with .NET 10 (Clean Architecture, DDD, CQRS/MediatR, MassTransit,
EF Core + PostgreSQL, OpenIddict) and React 19 + TypeScript + Ant Design.

🚀 **You can check out the live demo here:** [https://werkpilot.duckdns.org](https://werkpilot.duckdns.org)

---

## Quick start

Everything below works from a fresh clone. **No secrets, no API keys, no
accounts to register** — every external integration is optional and ships
disabled.

### Option A — Docker (recommended)

```bash
git clone https://github.com/ahmet191/WerkPilot.git
cd WerkPilot
docker compose up --build
```

Then open **<http://localhost:5173>** and sign in with a demo account below.
First boot applies migrations and seeds a demo tenant, so give it a moment.

This brings up PostgreSQL, RabbitMQ, Mailpit (a local mail catcher), the API
and the UI. Nothing leaves your machine.

| Service | URL |
|---|---|
| UI | <http://localhost:5173> |
| API | <http://localhost:5226> |
| Mailpit (sent mail) | <http://localhost:8025> |
| RabbitMQ management | <http://localhost:15672> (guest / guest) |

Reset everything, including the database:

```bash
docker compose down -v
```

### Option B — Run locally

Requires the **.NET 10 SDK**, **Node 24+**, and a PostgreSQL reachable at
`localhost:5432` with the default `postgres` / `postgres` credentials (the
compose file's `postgres` service will do: `docker compose up postgres`).

```bash
# terminal 1 — API on :5226
dotnet run --project src/OmniFlow.Api

# terminal 2 — UI on :5173
cd src/omniflow-ui && npm ci && npm run dev
```

In `Development` the API uses the in-memory message transport, so RabbitMQ is
not needed for this path. The Vite dev server proxies `/api`, `/connect` and
`/hubs` to the API, so there is nothing to configure in the frontend.

---

## Demo accounts

Seeded into the demo tenant on first boot, one per role, so the permission
model is explorable immediately.

| Account | Role | Password |
|---|---|---|
| `admin@werkpilot.com` | Administrator + PlatformAdmin | `Admin@123` |
| `bob@omniflow.com` | IT Agent | `Demo@12345` |
| `charlie@omniflow.com` | HR Admin | `Demo@12345` |
| `alice@omniflow.com` | Requester | `Demo@12345` |

These are development seed data. They exist only in a database you created
locally, and the seeder is the only thing that writes them — but do not point a
public deployment at a database seeded this way without changing them first.

---

## What's in it

- **Dynamic ticketing** — ticket templates define their own fields, form layout
  and state machine (Stateless). A visual workflow designer (React Flow) edits
  transitions, guards and approval policies without a deploy.
- **AI-assisted triage** — new tickets are classified for category and priority
  by Google Gemini and the prediction is persisted per ticket. With no API key
  it degrades to a keyword heuristic, so triage always returns a usable result;
  predictions are advisory and never mutate a ticket on their own.
- **Approval engine** — sequential, role- or manager-resolved approval chains
  driving both tickets and leave requests, with delegation support.
- **HRIS** — employees, departments, org chart, leave, assets and expense
  claims, plus a self-service employee centre and HR depth: performance goals /
  OKRs, a skills & certifications matrix, LMS training with assessment scoring,
  and onboarding checklists.
- **Analytics & reporting** — an executive KPI dashboard (SLA breach rate,
  first-time resolution, 30-day volume trend, priority mix) and cron-scheduled
  email reports, with CSV export from every analytics view.
- **No-deploy admin studios** — department studio, a dynamic ticket-form
  designer, an SLA escalation matrix, per-tenant permission and desk-visibility
  matrices, and UI-schema (grid layout) and interface-text editors — all
  configured at runtime.
- **Multi-tenancy** — enforced by an EF Core global query filter on every
  entity; tenants are provisioned from a platform console with their own
  seeded baseline.
- **Permission matrix** — a per-tenant role/permission grid materialized into
  ASP.NET Core authorization policies at request time.
- **Enterprise data grid** — server-side paging, sorting, multi-column
  filtering, search, grouping and export across every list view.
- **Notification centre** — in-app, SignalR-pushed, fanned out to email and
  optionally Telegram.
- **Durable messaging** — MassTransit with an EF Core transactional outbox, and
  Quartz-scheduled SLA jobs backed by PostgreSQL.
- **Auth** — OpenIddict (password / refresh / custom grants), TOTP MFA with
  recovery codes, optional external OIDC SSO and optional LDAP.
- **Localized, customizable interface** — English, Turkish and German out of the
  box, with dark/light theming; any UI string can be reworded per tenant, and
  shipped defaults stay in code so only overrides are stored.

## Architecture

```
src/
  OmniFlow.Domain/          entities, value objects, domain events — no dependencies
  OmniFlow.Application/     CQRS handlers, validators, interfaces
  OmniFlow.Infrastructure/  EF Core, Identity, messaging, integrations
  OmniFlow.Api/             controllers, auth, SignalR hubs, composition root
  omniflow-ui/              React 19 + Vite + TypeScript + Ant Design
tests/
  OmniFlow.Application.UnitTests/   handler and validator tests
  OmniFlow.Api.IntegrationTests/    full HTTP tests on a disposable Postgres
```

Dependencies point inward: Infrastructure never references the web layer.

## Tests

```bash
dotnet test                          # 93 tests (37 unit + 56 integration)
cd src/omniflow-ui && npm run build  # type-check + production build
```

Unit tests cover CQRS handlers and FluentValidation validators; integration
tests exercise the full HTTP stack — auth, RBAC, multi-tenant isolation, ticket
attachments, and the analytics/admin surfaces — against a real Postgres.

Integration tests start a throwaway PostgreSQL via Testcontainers, so Docker
must be running. CI runs the same commands on every push.

## Optional integrations

All disabled by default in `src/OmniFlow.Api/appsettings.json`. The application
boots and runs fully without any of them; each is enabled by supplying config.

| Integration | Enable with |
|---|---|
| AI ticket triage (Gemini) | `Gemini:ApiKey` — falls back to a keyword heuristic without it |
| Email (SMTP) | `Email:Host` — already pointed at Mailpit under Docker |
| External OIDC SSO | `Sso:Oidc:Enabled` + authority/client credentials |
| LDAP / Active Directory | `Ldap:Enabled` + host and bind settings |
| Telegram bot | `Telegram:BotToken` |
| RabbitMQ | `Messaging:Transport=RabbitMq` (`InMemory` otherwise) |
| OpenTelemetry export | `Otlp:Endpoint` |
| S3-compatible storage | `Storage:Provider=S3` + `Storage:S3:*` (see below) |

**Never put real credentials in `appsettings.json`** — it is committed. Use
user secrets for local development:

```bash
cd src/OmniFlow.Api
dotnet user-secrets set "Telegram:BotToken" "..."
```

or environment variables in production, where nested keys use a double
underscore: `Telegram__BotToken`.

## Production notes

The containers are built to run **non-root** (API as uid 1654, UI as uid 101)
and carry Docker `HEALTHCHECK`s. The UI's nginx listens on **8080** inside the
container (an unprivileged user cannot bind 80); compose maps host `5173` to it,
so the local URL is unchanged. When deploying the UI elsewhere, point the
platform's ingress/target-port at **8080**.

**File storage.** Ticket attachments default to the `Local` provider, which
writes to `/app/storage` — durable only if that path is a mounted volume (the
compose file mounts the `uploads` volume there). For portable durable storage,
use the `S3` provider against AWS or any S3-compatible endpoint (MinIO):

```
Storage__Provider=S3
Storage__S3__ServiceUrl=http://minio:9000   # omit for real AWS (uses Region)
Storage__S3__BucketName=werkpilot-uploads
Storage__S3__AccessKey=...
Storage__S3__SecretKey=...
```

`docker compose --profile s3 up` starts a local MinIO to try it against.

**Certificates.** Outside `Development`, OpenIddict requires real signing and
encryption certificates (the API refuses to start without them). Generate a
demo pair and point config at them:

```bash
scripts/generate-certs.sh ./certs
# then set OpenIddict__SigningCertificate__Path, __Password, and the
# EncryptionCertificate equivalents (mount ./certs into the container)
```

Swagger is served only when `Swagger:Enabled=true` (defaults on in Development,
off otherwise), so a demo left in Development mode still won't expose the API
explorer unless you opt in.

## Engineering Methodology & Author

**Author & System Architecture:** [Ahmet Sekerci](https://github.com/ahmet191)

WerkPilot was designed and built as a modern showcase of **AI-assisted enterprise software engineering**:
- **System Architecture & Governance:** Core architectural decisions (Clean Architecture, DDD, multi-tenant isolation, dynamic schema engines, OpenIddict OAuth2/OIDC, and event-driven patterns with MassTransit) were modeled and governed by Ahmet Sekerci.
- **AI Collaboration:** Implementation, test suite generation, and component scaffolding were executed in tight collaboration with **Claude Code** and AI agentic workflows.
- **Modern AI Workflow:** This repository demonstrates how strong software architecture, domain modeling, and hands-on direction combined with state-of-the-art AI tools produce robust, enterprise-ready platforms at high velocity.

---

## License

**Proprietary — all rights reserved © Ahmet Sekerci.**

This repository is published **solely as a portfolio and evaluation piece** — to
demonstrate the author's engineering work to prospective employers and
reviewers. It is **not** open-source.

You may **view and read** the source for personal evaluation only. You may **not**
fork, clone for reuse, copy, modify, redistribute, publish, sublicense, sell,
or otherwise use it — in whole or in part — for any commercial, for-profit, or
production purpose, nor incorporate any part of it into another work. Making the
source publicly visible grants **no** license or usage right of any kind.

For purchase or licensing enquiries, please
[contact the author](https://github.com/ahmet191).

See [LICENSE](LICENSE). Security policy: [SECURITY.md](SECURITY.md).

