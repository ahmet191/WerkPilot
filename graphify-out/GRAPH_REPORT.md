# Graph Report - .  (2026-07-12)

## Corpus Check
- 249 files · ~65,857 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1464 nodes · 2676 edges · 82 communities (75 shown, 7 thin omitted)
- Extraction: 97% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 64 edges (avg confidence: 0.79)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- React Frontend Shell + API Client
- Audit Log Queries + Grid Sorting
- Tickets Application + API
- Telegram Webhook + Notifications
- Assets Application + API
- Frontend UI Dependencies
- EF Core DbContext Interceptors
- Users Application + API
- HR Domain Persistence Configs
- Departments Application + API
- ASP.NET Identity + Auth
- Backend NuGet Dependencies
- Leave Requests Application + API
- UI Schema / Asset Schema API
- Change Request Domain
- Graphify Skill Docs + CLAUDE.md Integration
- Knowledge Base Application + API
- Templates API
- Problem / Service Desk Domain
- EF Core Migrations
- Frontend TypeScript Config
- DbContext Interface + Audit Log Entity
- MultiLingualString Value Object
- Messaging Consumers + Domain Events
- MediatR Pipeline Behaviors
- Vite Node Config
- Database Seeding
- EF Core Entity Configurations
- Dynamic Query Expression Building
- Menu Application + API
- Application Exceptions + API Error Model
- SLA Configurations API
- Leave Balance / Leave Request Domain
- ASP.NET Launch Settings
- Dashboard Application + API
- Service Desks API
- API Middleware (Audit/Exception)
- App Menu + SLA Domain Configs
- Generic CRUD API Endpoints
- Compensation Domain + Identity DbContext
- SignalR Dashboard Hub
- Tenant + Telegram Registration Services
- Configuration Item / Location Domain
- Graphify Pipeline Stages (SKILL.md)
- Base Entity + Domain Events
- Employment Details Domain
- Grid Result API Endpoints
- omniflow-ui HTML Entry Point
- SLA Sweep Background Service
- Directory + Localization API
- Telegram Bot Hosted Service
- Oxlint Frontend Config
- Lookup API (Categories/Departments)
- Correlation ID Middleware
- Database Health Check
- Expense Claims API + User Context
- Ticket Comment Command
- Asset Domain Entity + Config
- Graphify Extraction Spec Rules
- Audit Logs Controller
- Delete User Command
- Employee Document Domain
- Ticket Audit Log Domain
- omniflow-ui Icon Sprite Sheet
- Analytics API (KPIs)
- Directory API (Org Chart)
- Assign Ticket Command
- Team Domain Entity
- Ticket Comment Domain
- Graphify Cross-Repo / Monorepo Merge
- WerkPilot CLAUDE.md Graphify Wiring
- Graphify Watch Mode
- Graphify DB Exports (Neo4j/FalkorDB/MCP/Wiki)
- React + Vite Scaffold Assets
- Favicon + Brand Mark
- Frontend Root tsconfig
- Graphify Add URL Ingestion
- Graphify GraphML Export
- Graphify SVG Export
- Hero Banner Image
- Vite Logo Asset

## God Nodes (most connected - your core abstractions)
1. `OmniFlow.Domain.Entities` - 75 edges
2. `IOmniFlowDbContext` - 71 edges
3. `OmniFlow.Application.Interfaces` - 61 edges
4. `OmniFlow.Domain.Enums` - 47 edges
5. `OmniFlowDbContext` - 47 edges
6. `OmniFlow.Domain.Common` - 46 edges
7. `AuditableEntity` - 38 edges
8. `User` - 32 edges
9. `Ticket` - 24 edges
10. `OmniFlow.Api.Controllers` - 21 edges

## Surprising Connections (you probably didn't know these)
- `omniflow-ui index.html (favicon reference)` --references--> `Favicon Icon (favicon.svg)`  [AMBIGUOUS]
  /Users/ahmetsekerci/projects/WerkPilot/src/omniflow-ui/index.html → /Users/ahmetsekerci/projects/WerkPilot/src/omniflow-ui/public/favicon.svg
- `Ticket` --references--> `TicketPriority`  [EXTRACTED]
  src/OmniFlow.Domain/Entities/Ticket.cs → src/OmniFlow.Domain/Enums/TicketPriority.cs
- `User` --references--> `Gender`  [EXTRACTED]
  src/OmniFlow.Domain/Entities/User.cs → src/OmniFlow.Domain/Enums/Gender.cs
- `User` --references--> `MaritalStatus`  [EXTRACTED]
  src/OmniFlow.Domain/Entities/User.cs → src/OmniFlow.Domain/Enums/MaritalStatus.cs
- `Post-commit Hook` --semantically_similar_to--> `--update Incremental Re-extraction`  [INFERRED] [semantically similar]
  /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/hooks.md → /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/update.md

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Keeping the Graph Fresh After Changes** — claude_skills_graphify_references_hooks_post_commit_hook, claude_skills_graphify_references_update_incremental_update, claude_graphify_project_rules [INFERRED 0.85]
- **Query/Path/Explain CLI Trio with Shared Feedback Loop** — claude_skills_graphify_references_query_graphify_query_cli, claude_skills_graphify_references_query_graphify_path_cli, claude_skills_graphify_references_query_graphify_explain_cli, claude_skills_graphify_references_query_save_result [EXTRACTED 1.00]

## Communities (82 total, 7 thin omitted)

### Community 0 - "React Frontend Shell + API Client"
Cohesion: 0.06
Nodes (57): RFC-7807, react, api, unwrapList(), AuthContext, AuthContextType, AuthProvider(), DecodedToken (+49 more)

### Community 1 - "Audit Log Queries + Grid Sorting"
Cohesion: 0.05
Nodes (43): OmniFlow.Application.AuditLogs.Queries, OmniFlow.Application.Common.Models, OmniFlow.Application.Common.Grid, IComparer, IRequestHandler, NaturalComparer, CancellationToken, Guid (+35 more)

### Community 2 - "Tickets Application + API"
Cohesion: 0.07
Nodes (44): OmniFlow.Application.Tickets.Queries, IMessageScheduler, IRequest, ActionResult, Guid, HttpGet, HttpPost, IActionResult (+36 more)

### Community 3 - "Telegram Webhook + Notifications"
Cohesion: 0.06
Nodes (38): INotification, INotificationHandler, CancellationToken, HttpPost, IActionResult, ILogger, Task, TelegramWebhookController (+30 more)

### Community 4 - "Assets Application + API"
Cohesion: 0.07
Nodes (32): OmniFlow.Application.Assets.Commands, OmniFlow.Application.Assets.Queries, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult (+24 more)

### Community 5 - "Frontend UI Dependencies"
Cohesion: 0.05
Nodes (43): @ant-design/icons, antd, axios, jwt-decode, lucide-react, @microsoft/signalr, oxlint, react (+35 more)

### Community 6 - "EF Core DbContext Interceptors"
Cohesion: 0.06
Nodes (32): DbContext, DbContextEventData, IConsumer, InterceptionResult, SaveChangesInterceptor, Guid, ITenantService, DateTime (+24 more)

### Community 7 - "Users Application + API"
Cohesion: 0.07
Nodes (27): AbstractValidator, OmniFlow.Application.Users.Commands, ActionResult, Authorize, Guid, HttpDelete, HttpGet, HttpPost (+19 more)

### Community 8 - "HR Domain Persistence Configs"
Cohesion: 0.08
Nodes (27): OmniFlow.Infrastructure.Persistence.Configurations.HR, OmniFlow.Domain.Common, OmniFlow.Domain.Entities.HR, DateTimeOffset, Guid, AuditableEntity, DateTimeOffset, Guid (+19 more)

### Community 9 - "Departments Application + API"
Cohesion: 0.08
Nodes (27): OmniFlow.Application.Departments.Commands, OmniFlow.Application.Departments.Queries, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult (+19 more)

### Community 10 - "ASP.NET Identity + Auth"
Cohesion: 0.08
Nodes (26): ClaimsPrincipal, OmniFlow.Infrastructure.Identity, EnableRateLimiting, IdentityRole, IdentityUser, IgnoreAntiforgeryToken, OpenIddictRequest, Produces (+18 more)

### Community 11 - "Backend NuGet Dependencies"
Cohesion: 0.06
Nodes (32): FluentValidation (12.1.1), FluentValidation.DependencyInjectionExtensions (12.1.1), MassTransit (8.3.4), MassTransit.Abstractions (8.3.4), MassTransit.EntityFrameworkCore (8.3.4), MediatR (14.2.0), MediatR.Contracts (2.0.1), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.3) (+24 more)

### Community 12 - "Leave Requests Application + API"
Cohesion: 0.08
Nodes (26): OmniFlow.Application.LeaveRequests.Queries, OmniFlow.Application.LeaveRequests.Commands, Authorize, Guid, HttpGet, HttpPost, HttpPut, IActionResult (+18 more)

### Community 13 - "UI Schema / Asset Schema API"
Cohesion: 0.15
Nodes (14): Guid, HttpGet, IActionResult, Task, UiSchemaController, List, FieldOption, FormField (+6 more)

### Community 14 - "Change Request Domain"
Cohesion: 0.08
Nodes (17): OmniFlow.Domain.Enums, DateTime, Guid, ICollection, ChangeRequest, DateTime, Guid, ExpenseClaim (+9 more)

### Community 15 - "Graphify Skill Docs + CLAUDE.md Integration"
Cohesion: 0.08
Nodes (28): GRAPH_REPORT.md, graphify-out Directory, graphify Project Rules (WerkPilot CLAUDE.md), Native CLAUDE.md Integration, graphify claude install/uninstall, graphify hook install/uninstall/status, Post-commit Hook, BFS Traversal Mode (+20 more)

### Community 16 - "Knowledge Base Application + API"
Cohesion: 0.11
Nodes (21): OmniFlow.Application.KnowledgeBase.Queries, OmniFlow.Application.KnowledgeBase.Commands, HttpGet, HttpPost, IActionResult, IMediator, Task, KnowledgeBaseController (+13 more)

### Community 17 - "Templates API"
Cohesion: 0.12
Nodes (19): ActionResult, Authorize, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult (+11 more)

### Community 18 - "Problem / Service Desk Domain"
Cohesion: 0.08
Nodes (18): Guid, ICollection, Problem, Guid, ICollection, List, ServiceDesk, DateTimeOffset (+10 more)

### Community 19 - "EF Core Migrations"
Cohesion: 0.09
Nodes (13): OmniFlow.Infrastructure.Migrations, Migration, ModelSnapshot, MigrationBuilder, ModelBuilder, InitialBaseline, InitialBaseline, MigrationBuilder (+5 more)

### Community 20 - "Frontend TypeScript Config"
Cohesion: 0.08
Nodes (23): DOM, src, vite/client, compilerOptions, allowArbitraryExtensions, allowImportingTsExtensions, erasableSyntaxOnly, jsx (+15 more)

### Community 21 - "DbContext Interface + Audit Log Entity"
Cohesion: 0.10
Nodes (16): DbSet, IOmniFlowDbContext, DateTime, Guid, AuditLog, DateTime, Guid, AuthorityDelegation (+8 more)

### Community 22 - "MultiLingualString Value Object"
Cohesion: 0.11
Nodes (16): Dictionary, MultiLingualString, Guid, ICollection, Department, Guid, KnowledgeBaseArticle, Guid (+8 more)

### Community 23 - "Messaging Consumers + Domain Events"
Cohesion: 0.15
Nodes (10): OmniFlow.Infrastructure.Messaging.Consumers, OmniFlow.Application.Interfaces, OmniFlow.Infrastructure.Persistence.Interceptors, OmniFlow.Domain.Events, OmniFlow.Application.Tickets.EventHandlers, OmniFlow.Infrastructure.Messaging, OmniFlow.Infrastructure.Persistence, OmniFlow.Infrastructure.Notifications (+2 more)

### Community 24 - "MediatR Pipeline Behaviors"
Cohesion: 0.10
Nodes (16): OmniFlow.Application.Common.Behaviors, OmniFlow.Application, IPipelineBehavior, IServiceCollection, CancellationToken, ILogger, int, RequestHandlerDelegate (+8 more)

### Community 25 - "Vite Node Config"
Cohesion: 0.10
Nodes (19): node, vite.config.ts, compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection (+11 more)

### Community 26 - "Database Seeding"
Cohesion: 0.19
Nodes (11): OmniFlow.Infrastructure.Persistence.Seed, CancellationToken, IServiceProvider, OmniFlowDbContext, Task, DatabaseSeeder, IServiceProvider, Task (+3 more)

### Community 28 - "Dynamic Query Expression Building"
Cohesion: 0.18
Nodes (10): Expression, ParameterExpression, PropertyInfo, CancellationToken, Dictionary, IQueryable, List, Task (+2 more)

### Community 29 - "Menu Application + API"
Cohesion: 0.18
Nodes (13): OmniFlow.Application.Menus.Queries, HttpGet, IActionResult, IMediator, Task, MenuController, CancellationToken, Guid (+5 more)

### Community 30 - "Application Exceptions + API Error Model"
Cohesion: 0.16
Nodes (9): OmniFlow.Application.Common.Exceptions, OmniFlow.Application.Tickets.Commands, Exception, IDictionary, ApiException, ConflictException, ForbiddenAccessException, NotFoundException (+1 more)

### Community 31 - "SLA Configurations API"
Cohesion: 0.20
Nodes (11): Authorize, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, Task (+3 more)

### Community 32 - "Leave Balance / Leave Request Domain"
Cohesion: 0.13
Nodes (11): Guid, LeaveBalance, DateTime, Guid, LeaveRequest, LeaveStatus, LeaveType, EntityTypeBuilder (+3 more)

### Community 33 - "ASP.NET Launch Settings"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 34 - "Dashboard Application + API"
Cohesion: 0.17
Nodes (11): OmniFlow.Application.Dashboard.Queries, HttpGet, IActionResult, IMediator, Task, DashboardController, CancellationToken, Dictionary (+3 more)

### Community 35 - "Service Desks API"
Cohesion: 0.24
Nodes (10): Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, Task, CreateServiceDeskRequest (+2 more)

### Community 36 - "API Middleware (Audit/Exception)"
Cohesion: 0.18
Nodes (10): OmniFlow.Api.Middleware, JsonSerializerOptions, RequestDelegate, AuditLoggingMiddleware, Exception, HttpContext, ILogger, RequestDelegate (+2 more)

### Community 37 - "App Menu + SLA Domain Configs"
Cohesion: 0.16
Nodes (10): IEntityTypeConfiguration, Guid, ICollection, AppMenu, Guid, SLAConfiguration, EntityTypeBuilder, AppMenuConfiguration (+2 more)

### Community 38 - "Generic CRUD API Endpoints"
Cohesion: 0.26
Nodes (10): Authorize, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, Task (+2 more)

### Community 39 - "Compensation Domain + Identity DbContext"
Cohesion: 0.17
Nodes (9): DbContextOptionsBuilder, IdentityDbContext, DateTime, Guid, Compensation, DbSet, Guid, ModelBuilder (+1 more)

### Community 40 - "SignalR Dashboard Hub"
Cohesion: 0.17
Nodes (8): OmniFlow.Api.Hubs, Hub, IHubContext, Task, DashboardHub, CancellationToken, Task, DashboardMetricsService

### Community 41 - "Tenant + Telegram Registration Services"
Cohesion: 0.18
Nodes (8): OmniFlow.Api.Services, Guid, IConfiguration, IHttpContextAccessor, TenantService, Guid, IHttpContextAccessor, UserContextService

### Community 42 - "Configuration Item / Location Domain"
Cohesion: 0.18
Nodes (7): Guid, ConfigurationItem, Location, CIStatus, CIType, EntityTypeBuilder, LocationConfiguration

### Community 43 - "Graphify Pipeline Stages (SKILL.md)"
Cohesion: 0.18
Nodes (11): graphify SKILL.md, Build Graph and Cluster Communities, Detect Files, Export Formats (Obsidian, Neo4j, Wiki, SVG), Generate Visualization, Label Communities, Manifest and Cost Tracking, Query / Path / Explain (+3 more)

### Community 44 - "Base Entity + Domain Events"
Cohesion: 0.22
Nodes (7): IReadOnlyCollection, Guid, INotification, List, BaseEntity, DateTime, Tenant

### Community 45 - "Employment Details Domain"
Cohesion: 0.20
Nodes (7): DateTime, Guid, EmploymentDetails, EmploymentStatus, EmploymentType, EntityTypeBuilder, EmploymentDetailsConfiguration

### Community 46 - "Grid Result API Endpoints"
Cohesion: 0.20
Nodes (7): HttpGet, IActionResult, Task, IEnumerable, Dictionary, int, GridRequest

### Community 47 - "omniflow-ui HTML Entry Point"
Cohesion: 0.20
Nodes (10): omniflow-ui HTML Entry Point, main.tsx Script, #root div, Oxc, Oxlint Configuration (.oxlintrc.json), React Compiler, SWC, React + TypeScript + Vite Template (+2 more)

### Community 48 - "SLA Sweep Background Service"
Cohesion: 0.31
Nodes (7): BackgroundService, IServiceScopeFactory, CancellationToken, ILogger, Task, SlaSweepBackgroundService, TimeSpan

### Community 49 - "Directory + Localization API"
Cohesion: 0.22
Nodes (5): OmniFlow.Api.Controllers, Dictionary, HttpGet, IActionResult, LocalizationController

### Community 50 - "Telegram Bot Hosted Service"
Cohesion: 0.28
Nodes (7): IHostedService, CancellationToken, IConfiguration, ILogger, ITelegramBotClient, Task, TelegramWebhookRegistrationService

### Community 51 - "Oxlint Frontend Config"
Cohesion: 0.22
Nodes (8): oxc, typescript, warn, plugins, rules, react/only-export-components, react/rules-of-hooks, $schema

### Community 52 - "Lookup API (Categories/Departments)"
Cohesion: 0.53
Nodes (4): HttpGet, IActionResult, Task, LookupController

### Community 53 - "Correlation ID Middleware"
Cohesion: 0.28
Nodes (6): HttpContext, ILogger, RequestDelegate, Task, CorrelationIdMiddleware, string

### Community 54 - "Database Health Check"
Cohesion: 0.25
Nodes (6): HealthCheckContext, HealthCheckResult, IHealthCheck, CancellationToken, Task, DatabaseHealthCheck

### Community 55 - "Expense Claims API + User Context"
Cohesion: 0.25
Nodes (5): ExpenseClaimsController, HttpContext, Task, Guid, IUserContext

### Community 56 - "Ticket Comment Command"
Cohesion: 0.32
Nodes (6): Guid, AddTicketCommentCommand, CancellationToken, Guid, Task, AddTicketCommentCommandHandler

### Community 57 - "Asset Domain Entity + Config"
Cohesion: 0.29
Nodes (5): Guid, Asset, AssetStatus, EntityTypeBuilder, AssetConfiguration

### Community 58 - "Graphify Extraction Spec Rules"
Cohesion: 0.29
Nodes (7): Extraction Spec, EXTRACTED/INFERRED/AMBIGUOUS Classification, Confidence Score Rubric, Hyperedge Rules, Node ID Format, Semantic Similarity Edges, Vision Extraction Rules

### Community 59 - "Audit Logs Controller"
Cohesion: 0.29
Nodes (6): ControllerBase, HttpGet, IActionResult, IMediator, Task, AuditLogsController

### Community 60 - "Delete User Command"
Cohesion: 0.38
Nodes (5): CancellationToken, Guid, Task, DeleteUserCommand, DeleteUserCommandHandler

### Community 61 - "Employee Document Domain"
Cohesion: 0.33
Nodes (5): DateTime, Guid, EmployeeDocument, EntityTypeBuilder, EmployeeDocumentConfiguration

### Community 62 - "Ticket Audit Log Domain"
Cohesion: 0.33
Nodes (5): DateTimeOffset, Guid, TicketAuditLog, EntityTypeBuilder, TicketAuditLogConfiguration

### Community 63 - "omniflow-ui Icon Sprite Sheet"
Cohesion: 0.71
Nodes (7): icons.svg Sprite Sheet, Bluesky Icon, Discord Icon, Documentation Icon, GitHub Icon, Social (Contacts/Network) Icon, X (Twitter) Icon

### Community 64 - "Analytics API (KPIs)"
Cohesion: 0.33
Nodes (4): HttpGet, IActionResult, Task, AnalyticsController

### Community 65 - "Directory API (Org Chart)"
Cohesion: 0.53
Nodes (4): HttpGet, IActionResult, Task, DirectoryController

### Community 66 - "Assign Ticket Command"
Cohesion: 0.40
Nodes (4): AssignTicketCommand, CancellationToken, Task, AssignTicketCommandHandler

### Community 67 - "Team Domain Entity"
Cohesion: 0.40
Nodes (4): Guid, Team, EntityTypeBuilder, TeamConfiguration

### Community 68 - "Ticket Comment Domain"
Cohesion: 0.40
Nodes (4): Guid, TicketComment, EntityTypeBuilder, TicketCommentConfiguration

### Community 69 - "Graphify Cross-Repo / Monorepo Merge"
Cohesion: 0.50
Nodes (5): Cross-repo graph workflow, graphify clone <github-url>, graphify extract <path>, graphify merge-graphs, Monorepo / multi-service layout workflow

### Community 70 - "WerkPilot CLAUDE.md Graphify Wiring"
Cohesion: 0.50
Nodes (4): WerkPilot .claude/CLAUDE.md, graphify skill, graphify SKILL.md reference, /graphify trigger command

### Community 71 - "Graphify Watch Mode"
Cohesion: 0.50
Nodes (4): --debounce flag, graphify.watch (--watch), needs_update flag, Token reduction benchmark

### Community 72 - "Graphify DB Exports (Neo4j/FalkorDB/MCP/Wiki)"
Cohesion: 0.50
Nodes (4): FalkorDB export (--falkordb / --falkordb-push), MCP server (--mcp), Neo4j export (--neo4j / --neo4j-push), Wiki export (--wiki)

### Community 73 - "React + Vite Scaffold Assets"
Cohesion: 0.67
Nodes (3): React (JavaScript Library), Vite (Build Tool Scaffold), React Logo (Vite Scaffold Asset)

### Community 74 - "Favicon + Brand Mark"
Cohesion: 0.67
Nodes (3): Favicon Icon (favicon.svg), omniflow-ui index.html (favicon reference), OmniFlow/WerkPilot Brand Mark

## Ambiguous Edges - Review These
- `Favicon Icon (favicon.svg)` → `omniflow-ui index.html (favicon reference)`  [AMBIGUOUS]
  /Users/ahmetsekerci/projects/WerkPilot/src/omniflow-ui/public/favicon.svg · relation: references
- `Documentation Icon` → `GitHub Icon`  [AMBIGUOUS]
  /Users/ahmetsekerci/projects/WerkPilot/src/omniflow-ui/public/icons.svg · relation: conceptually_related_to
- `graphify.watch (--watch)` → `Token reduction benchmark`  [AMBIGUOUS]
  /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/add-watch.md · relation: conceptually_related_to
- `Neo4j export (--neo4j / --neo4j-push)` → `MCP server (--mcp)`  [AMBIGUOUS]
  /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/exports.md · relation: conceptually_related_to

## Knowledge Gaps
- **185 isolated node(s):** `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.2)`, `Microsoft.EntityFrameworkCore.Design (10.0.9)`, `OpenTelemetry.Exporter.OpenTelemetryProtocol (1.16.0)`, `OpenTelemetry.Extensions.Hosting (1.16.0)` (+180 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **7 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What is the exact relationship between `Favicon Icon (favicon.svg)` and `omniflow-ui index.html (favicon reference)`?**
  _Edge tagged AMBIGUOUS (relation: references) - confidence is low._
- **What is the exact relationship between `Documentation Icon` and `GitHub Icon`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **What is the exact relationship between `graphify.watch (--watch)` and `Token reduction benchmark`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **What is the exact relationship between `Neo4j export (--neo4j / --neo4j-push)` and `MCP server (--mcp)`?**
  _Edge tagged AMBIGUOUS (relation: conceptually_related_to) - confidence is low._
- **Why does `IOmniFlowDbContext` connect `DbContext Interface + Audit Log Entity` to `Audit Log Queries + Grid Sorting`, `Tickets Application + API`, `Assets Application + API`, `EF Core DbContext Interceptors`, `Users Application + API`, `Departments Application + API`, `Leave Requests Application + API`, `UI Schema / Asset Schema API`, `Change Request Domain`, `Knowledge Base Application + API`, `Templates API`, `Problem / Service Desk Domain`, `MultiLingualString Value Object`, `Menu Application + API`, `SLA Configurations API`, `Leave Balance / Leave Request Domain`, `Dashboard Application + API`, `Service Desks API`, `App Menu + SLA Domain Configs`, `Generic CRUD API Endpoints`, `Compensation Domain + Identity DbContext`, `Configuration Item / Location Domain`, `Employment Details Domain`, `Lookup API (Categories/Departments)`, `Expense Claims API + User Context`, `Ticket Comment Command`, `Asset Domain Entity + Config`, `Delete User Command`, `Employee Document Domain`, `Ticket Audit Log Domain`, `Analytics API (KPIs)`, `Directory API (Org Chart)`, `Assign Ticket Command`, `Team Domain Entity`, `Ticket Comment Domain`?**
  _High betweenness centrality (0.195) - this node is a cross-community bridge._
- **Why does `OmniFlow.Application.Interfaces` connect `Messaging Consumers + Domain Events` to `Audit Log Queries + Grid Sorting`, `Tickets Application + API`, `Telegram Webhook + Notifications`, `Assets Application + API`, `EF Core DbContext Interceptors`, `Users Application + API`, `Departments Application + API`, `ASP.NET Identity + Auth`, `Leave Requests Application + API`, `Change Request Domain`, `Knowledge Base Application + API`, `Templates API`, `DbContext Interface + Audit Log Entity`, `MediatR Pipeline Behaviors`, `Menu Application + API`, `Application Exceptions + API Error Model`, `SLA Configurations API`, `Dashboard Application + API`, `Service Desks API`, `API Middleware (Audit/Exception)`, `SignalR Dashboard Hub`, `Tenant + Telegram Registration Services`, `Directory + Localization API`, `Expense Claims API + User Context`, `Delete User Command`, `Analytics API (KPIs)`?**
  _High betweenness centrality (0.099) - this node is a cross-community bridge._
- **Why does `OmniFlowDbContext` connect `Compensation Domain + Identity DbContext` to `EF Core DbContext Interceptors`, `HR Domain Persistence Configs`, `ASP.NET Identity + Auth`, `Change Request Domain`, `Templates API`, `Problem / Service Desk Domain`, `DbContext Interface + Audit Log Entity`, `MultiLingualString Value Object`, `Messaging Consumers + Domain Events`, `Leave Balance / Leave Request Domain`, `App Menu + SLA Domain Configs`, `Configuration Item / Location Domain`, `Base Entity + Domain Events`, `Employment Details Domain`, `Database Health Check`, `Asset Domain Entity + Config`, `Employee Document Domain`, `Ticket Audit Log Domain`, `Team Domain Entity`, `Ticket Comment Domain`?**
  _High betweenness centrality (0.070) - this node is a cross-community bridge._