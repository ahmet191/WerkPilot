# WerkPilot — OmniFlow ESM

Multi-tenant Enterprise Service Management: an HRIS and a dynamic ITSM/ticketing
platform in one application. Ticket types, forms, workflows and interface text
are configured per tenant at runtime rather than compiled in.

Built with .NET 10 (Clean Architecture, DDD, CQRS/MediatR, MassTransit,
EF Core + PostgreSQL, OpenIddict) and React 19 + TypeScript + Ant Design.

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
- **Approval engine** — sequential, role- or manager-resolved approval chains
  driving both tickets and leave requests, with delegation support.
- **HRIS** — employees, departments, org chart, leave, assets, expense claims.
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
- **Customizable interface text** — any UI string can be reworded per tenant;
  shipped defaults stay in code, only overrides are stored.

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
dotnet test                          # 73 tests
cd src/omniflow-ui && npm run build  # type-check + production build
```

Integration tests start a throwaway PostgreSQL via Testcontainers, so Docker
must be running. CI runs the same commands on every push.

## Optional integrations

All disabled by default in `src/OmniFlow.Api/appsettings.json`. The application
boots and runs fully without any of them; each is enabled by supplying config.

| Integration | Enable with |
|---|---|
| Email (SMTP) | `Email:Host` — already pointed at Mailpit under Docker |
| External OIDC SSO | `Sso:Oidc:Enabled` + authority/client credentials |
| LDAP / Active Directory | `Ldap:Enabled` + host and bind settings |
| Telegram bot | `Telegram:BotToken` |
| RabbitMQ | `Messaging:Transport=RabbitMq` (`InMemory` otherwise) |
| OpenTelemetry export | `Otlp:Endpoint` |

**Never put real credentials in `appsettings.json`** — it is committed. Use
user secrets for local development:

```bash
cd src/OmniFlow.Api
dotnet user-secrets set "Telegram:BotToken" "..."
```

or environment variables in production, where nested keys use a double
underscore: `Telegram__BotToken`.

## License

No license has been chosen yet. Until one is added, default copyright applies
and the code is not licensed for reuse.
