# Graph Report - .  (2026-07-13)

## Corpus Check
- 34 files · ~74,431 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1672 nodes · 2903 edges · 116 communities (105 shown, 11 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 68 edges (avg confidence: 0.79)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- EF Core Interceptors + API Actions
- Frontend NPM Dependencies
- HR & Identity Domain Entities
- SignalR Hubs + Health Checks
- Auditable Entity + Domain Commands
- Leave Requests + Audit Log Queries
- ASP.NET Identity + Auth
- Backend NuGet Dependencies
- Tenant Service + Leave Domain
- Approvals Application + API
- EF Core Migrations
- UI Schema API
- Message Scheduler + Controllers
- Localization + Controller Actions
- Graphify Skill Docs + CLAUDE.md Integration
- Core Domain Entities
- HR Domain Persistence Configs
- Approvals + Assets Domain
- MultiLingualString Value Object
- Event Consumers + Messaging
- Departments Application + API
- Exceptions + Tickets/Users Commands
- Notification Gateway
- Frontend TS Config
- Approvals UI Feature
- Approval Validators
- Tenant Service + Asset Events
- Approval Domain Events
- Tickets Queries + Grid Models
- Dynamic Form Schema (Frontend)
- Knowledge Base Application + API
- API Middleware
- Frontend Build Config
- Menus Application + API
- HR Compensation + Directory
- Ticket Domain Value Objects
- MediatR Pipeline Behaviors
- Generic Controller Actions
- Dynamic Query Expression Builder
- Authorized Controller Actions
- ASP.NET launchSettings Config
- Dashboard Application + API
- Ticket Persistence Configs
- Telegram Webhook Controller
- AuditableEntity + SoftDelete Base
- Leave Domain Entities
- Generic CRUD Controller
- Authorized CRUD Controller
- Expense Claims API
- Ticket Queries
- Ticket Details Query
- Telegram Bot Service
- Language + Theme Context (Frontend)
- Analytics API
- Generic Authorized Controller
- Graphify Skill Pipeline Steps
- API Client + Employee Directory (Frontend)
- Database Seeder
- Auth Context (Frontend)
- App Startup + SLA Sweep Service
- Assets Application + API
- MediatR Request + Tenant Context Interfaces
- Change Request Domain
- Configuration Item (CMDB) Domain
- Employment Domain Enums
- Leave Request Domain Events
- Frontend App Entry Point
- SLA Sweep Background Service
- BaseEntity Domain Base Class
- Oxlint Config (Frontend)
- Problem Management Domain
- Transition Ticket Command
- Assign Asset Command
- Add Ticket Comment Command
- Graphify Extraction Spec Docs
- Update Asset Command
- Delete Department Command
- Create KB Article Command
- Create User Command
- Asset Status + Persistence Config
- Icon Sprite Sheet (Frontend Assets)
- Enterprise Data Grid (Frontend)
- Tenant Entity + Config
- Localization API
- Employee Directory API
- Menu API
- Create Asset Command
- Create Department Command
- Assign Ticket Command
- Delete User Command
- Update User Command
- Employee Document Persistence Config
- Graphify Cross-repo Workflow Docs
- Auditable Entity Interceptor
- Application DI Registration
- Dashboard Metrics Broadcast Service
- HR Frontend Models + Service
- Graphify Skill Trigger Docs
- Graphify Watch Mode Docs
- Graphify Export Formats Docs
- Natural Sort Comparer Utility
- Identity Seed Data
- React + Vite Scaffold Docs
- DbContext Model Configuration
- Brand Assets Docs
- Root TS Config
- Graphify Add/Ingest Docs
- DbContext Configuration
- Graphify GraphML Export Doc
- Graphify SVG Export Doc
- Dictionary Type Reference
- IServiceProvider Type Reference
- OmniFlowDbContext Type Reference
- Hero Banner Image Asset
- Vite Logo Asset

## God Nodes (most connected - your core abstractions)
1. `IOmniFlowDbContext` - 80 edges
2. `OmniFlow.Domain.Entities` - 76 edges
3. `OmniFlow.Application.Interfaces` - 67 edges
4. `OmniFlow.Domain.Enums` - 54 edges
5. `OmniFlow.Domain.Common` - 52 edges
6. `OmniFlowDbContext` - 49 edges
7. `AuditableEntity` - 37 edges
8. `User` - 30 edges
9. `OmniFlow.Api.Controllers` - 22 edges
10. `Ticket` - 22 edges

## Surprising Connections (you probably didn't know these)
- `omniflow-ui index.html (favicon reference)` --references--> `Favicon Icon (favicon.svg)`  [AMBIGUOUS]
  /Users/ahmetsekerci/projects/WerkPilot/src/omniflow-ui/index.html → /Users/ahmetsekerci/projects/WerkPilot/src/omniflow-ui/public/favicon.svg
- `Post-commit Hook` --semantically_similar_to--> `--update Incremental Re-extraction`  [INFERRED] [semantically similar]
  /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/hooks.md → /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/update.md
- `Constrained Query Expansion` --semantically_similar_to--> `God Node Domain Hint Strategy`  [INFERRED] [semantically similar]
  /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/query.md → /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/transcribe.md
- `graphify.watch (--watch)` --conceptually_related_to--> `Token reduction benchmark`  [AMBIGUOUS]
  /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/add-watch.md → /Users/ahmetsekerci/projects/WerkPilot/.claude/skills/graphify/references/exports.md
- `AnalyticsController` --references--> `IOmniFlowDbContext`  [EXTRACTED]
  src/OmniFlow.Api/Controllers/AnalyticsController.cs → src/OmniFlow.Application/Interfaces/IOmniFlowDbContext.cs

## Import Cycles
- None detected.

## Hyperedges (group relationships)
- **Keeping the Graph Fresh After Changes** — claude_skills_graphify_references_hooks_post_commit_hook, claude_skills_graphify_references_update_incremental_update, claude_graphify_project_rules [INFERRED 0.85]
- **Query/Path/Explain CLI Trio with Shared Feedback Loop** — claude_skills_graphify_references_query_graphify_query_cli, claude_skills_graphify_references_query_graphify_path_cli, claude_skills_graphify_references_query_graphify_explain_cli, claude_skills_graphify_references_query_save_result [EXTRACTED 1.00]

## Communities (116 total, 11 thin omitted)

### Community 0 - "EF Core Interceptors + API Actions"
Cohesion: 0.07
Nodes (30): DbContext, DbContextEventData, InterceptionResult, SaveChangesInterceptor, HttpGet, IActionResult, Task, LookupController (+22 more)

### Community 1 - "Frontend NPM Dependencies"
Cohesion: 0.05
Nodes (43): @ant-design/icons, antd, axios, jwt-decode, lucide-react, @microsoft/signalr, oxlint, react (+35 more)

### Community 2 - "HR & Identity Domain Entities"
Cohesion: 0.05
Nodes (42): ApplicationRole, ApplicationUser, AttendanceRecord, AuditableEntityInterceptor, Benefit, Compensation, Employee, IdentityDbContext (+34 more)

### Community 3 - "SignalR Hubs + Health Checks"
Cohesion: 0.05
Nodes (29): OmniFlow.Api.Hubs, OmniFlow.Api.Services, HealthCheckContext, HealthCheckResult, Hub, IHealthCheck, IHostedService, IHubContext (+21 more)

### Community 4 - "Auditable Entity + Domain Commands"
Cohesion: 0.10
Nodes (3): OmniFlow.Domain.Common, OmniFlow.Infrastructure.Persistence.Configurations, OmniFlow.Domain.Entities

### Community 5 - "Leave Requests + Audit Log Queries"
Cohesion: 0.08
Nodes (30): OmniFlow.Application.LeaveRequests.Queries, OmniFlow.Application.AuditLogs.Queries, CancellationToken, Guid, Task, AssetDto, GetAssetsQuery, GetAssetsQueryHandler (+22 more)

### Community 6 - "ASP.NET Identity + Auth"
Cohesion: 0.08
Nodes (26): ClaimsPrincipal, OmniFlow.Infrastructure.Identity, EnableRateLimiting, IdentityRole, IdentityUser, IgnoreAntiforgeryToken, OpenIddictRequest, Produces (+18 more)

### Community 7 - "Backend NuGet Dependencies"
Cohesion: 0.06
Nodes (32): FluentValidation (12.1.1), FluentValidation.DependencyInjectionExtensions (12.1.1), MassTransit (8.3.4), MassTransit.Abstractions (8.3.4), MassTransit.EntityFrameworkCore (8.3.4), MediatR (14.2.0), MediatR.Contracts (2.0.1), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.3) (+24 more)

### Community 8 - "Tenant Service + Leave Domain"
Cohesion: 0.08
Nodes (29): AuditableEntity, LeaveType, CancellationToken, Guid, IPublishEndpoint, ITenantService, Task, ApprovalRequestBuilder (+21 more)

### Community 9 - "Approvals Application + API"
Cohesion: 0.09
Nodes (28): OmniFlow.Application.Approvals.Queries, OmniFlow.Application.Approvals.Commands, HttpPost, Guid, HttpGet, IActionResult, IMediator, Task (+20 more)

### Community 10 - "EF Core Migrations"
Cohesion: 0.07
Nodes (17): OmniFlow.Infrastructure.Migrations, Migration, MigrationBuilder, ModelSnapshot, MigrationBuilder, ModelBuilder, InitialBaseline, InitialBaseline (+9 more)

### Community 11 - "UI Schema API"
Cohesion: 0.15
Nodes (14): Guid, HttpGet, IActionResult, Task, UiSchemaController, List, FieldOption, FormField (+6 more)

### Community 12 - "Message Scheduler + Controllers"
Cohesion: 0.13
Nodes (20): IMessageScheduler, ActionResult, Guid, HttpGet, HttpPost, IActionResult, IMediator, List (+12 more)

### Community 13 - "Localization + Controller Actions"
Cohesion: 0.12
Nodes (22): MultiLingualString, ActionResult, Authorize, Guid, HttpDelete, HttpGet, HttpPost, HttpPut (+14 more)

### Community 14 - "Graphify Skill Docs + CLAUDE.md Integration"
Cohesion: 0.08
Nodes (27): GRAPH_REPORT.md, graphify-out Directory, graphify Project Rules (WerkPilot CLAUDE.md), Native CLAUDE.md Integration, graphify claude install/uninstall, graphify hook install/uninstall/status, Post-commit Hook, BFS Traversal Mode (+19 more)

### Community 15 - "Core Domain Entities"
Cohesion: 0.08
Nodes (26): AppMenu, Asset, AuditLog, AuthorityDelegation, ChangeRequest, ConfigurationItem, DbSet, Department (+18 more)

### Community 16 - "HR Domain Persistence Configs"
Cohesion: 0.09
Nodes (19): OmniFlow.Infrastructure.Persistence.Configurations.HR, OmniFlow.Domain.Entities.HR, DateTime, Guid, AttendanceRecord, DateTime, Guid, Benefit (+11 more)

### Community 17 - "Approvals + Assets Domain"
Cohesion: 0.08
Nodes (9): OmniFlow.Domain.Enums, string, ApprovalEntityTypes, DateTime, Guid, ExpenseClaim, DocumentStatus, ExpenseCategory (+1 more)

### Community 18 - "MultiLingualString Value Object"
Cohesion: 0.10
Nodes (20): Dictionary, MultiLingualString, Guid, ICollection, Department, Guid, KnowledgeBaseArticle, Guid (+12 more)

### Community 19 - "Event Consumers + Messaging"
Cohesion: 0.15
Nodes (8): OmniFlow.Infrastructure.Messaging.Consumers, OmniFlow.Application.Interfaces, OmniFlow.Domain.Events, OmniFlow.Application.Tickets.EventHandlers, OmniFlow.Domain.Entities.Approvals, OmniFlow.Application.LeaveRequests.Commands, OmniFlow.Application.Approvals, OmniFlow.Infrastructure.Notifications

### Community 20 - "Departments Application + API"
Cohesion: 0.11
Nodes (17): OmniFlow.Application.Departments.Commands, OmniFlow.Application.Departments.Queries, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult (+9 more)

### Community 21 - "Exceptions + Tickets/Users Commands"
Cohesion: 0.11
Nodes (11): OmniFlow.Application.Common.Exceptions, OmniFlow.Application.Tickets.Commands, OmniFlow.Infrastructure.Messaging, OmniFlow.Application.Users.Commands, Exception, IDictionary, ApiException, ConflictException (+3 more)

### Community 22 - "Notification Gateway"
Cohesion: 0.13
Nodes (18): INotification, INotificationHandler, CancellationToken, Task, IMessageGateway, CancellationToken, Task, TicketCreatedEventHandler (+10 more)

### Community 23 - "Frontend TS Config"
Cohesion: 0.08
Nodes (23): DOM, src, vite/client, compilerOptions, allowArbitraryExtensions, allowImportingTsExtensions, erasableSyntaxOnly, jsx (+15 more)

### Community 24 - "Approvals UI Feature"
Cohesion: 0.12
Nodes (16): ApprovalTimeline(), STATUS_COLORS, stepDot(), TimelineEntry, TimelineStep, ENTITY_COLORS, MyApprovals(), PendingApproval (+8 more)

### Community 25 - "Approval Validators"
Cohesion: 0.11
Nodes (16): AbstractValidator, ApprovalStep, CancellationToken, Guid, IPublishEndpoint, ITenantService, IUserContext, Task (+8 more)

### Community 26 - "Tenant Service + Asset Events"
Cohesion: 0.11
Nodes (17): IConsumer, Guid, ITenantService, DateTime, Guid, AssetAssignedEvent, DateTime, Guid (+9 more)

### Community 27 - "Approval Domain Events"
Cohesion: 0.13
Nodes (18): IMessageGateway, Guid, ApprovalCompletedEvent, ApprovalStepActivatedEvent, CancellationToken, ConsumeContext, ILogger, IMediator (+10 more)

### Community 28 - "Tickets Queries + Grid Models"
Cohesion: 0.17
Nodes (6): OmniFlow.Application.Tickets.Queries, OmniFlow.Api.Controllers, OmniFlow.Application.Common.Models, OmniFlow.Application.Common.Grid, Dictionary, PagedRequest

### Community 29 - "Dynamic Form Schema (Frontend)"
Cohesion: 0.17
Nodes (17): useTranslation(), FieldOption, FormField, FormSchema, GridAction, GridColumn, GridSchema, UiSchema (+9 more)

### Community 30 - "Knowledge Base Application + API"
Cohesion: 0.15
Nodes (15): OmniFlow.Application.KnowledgeBase.Queries, OmniFlow.Application.KnowledgeBase.Commands, HttpGet, HttpPost, IActionResult, IMediator, Task, KnowledgeBaseController (+7 more)

### Community 31 - "API Middleware"
Cohesion: 0.13
Nodes (14): OmniFlow.Api.Middleware, JsonSerializerOptions, HttpContext, ILogger, RequestDelegate, Task, CorrelationIdMiddleware, Exception (+6 more)

### Community 32 - "Frontend Build Config"
Cohesion: 0.10
Nodes (19): node, vite.config.ts, compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection (+11 more)

### Community 33 - "Menus Application + API"
Cohesion: 0.18
Nodes (13): OmniFlow.Application.Menus.Queries, CancellationToken, Guid, List, Task, GetMenuQuery, GetMenuQueryHandler, MenuDto (+5 more)

### Community 34 - "HR Compensation + Directory"
Cohesion: 0.12
Nodes (14): DateTime, Guid, Compensation, Guid, List, DirectoryProfile, DateTime, Guid (+6 more)

### Community 35 - "Ticket Domain Value Objects"
Cohesion: 0.12
Nodes (12): DateTimeOffset, Dictionary, Guid, List, Ticket, TicketImpact, TicketPriority, TicketState (+4 more)

### Community 36 - "MediatR Pipeline Behaviors"
Cohesion: 0.12
Nodes (13): OmniFlow.Application.Common.Behaviors, IPipelineBehavior, CancellationToken, ILogger, int, RequestHandlerDelegate, Task, LoggingBehavior (+5 more)

### Community 37 - "Generic Controller Actions"
Cohesion: 0.20
Nodes (12): Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, Task, CreateServiceDeskRequest (+4 more)

### Community 38 - "Dynamic Query Expression Builder"
Cohesion: 0.22
Nodes (8): Expression, ParameterExpression, PropertyInfo, Dictionary, IQueryable, List, GridQueryExtensions, Type

### Community 39 - "Authorized Controller Actions"
Cohesion: 0.23
Nodes (11): ActionResult, Authorize, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult (+3 more)

### Community 40 - "ASP.NET launchSettings Config"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 41 - "Dashboard Application + API"
Cohesion: 0.17
Nodes (11): OmniFlow.Application.Dashboard.Queries, HttpGet, IActionResult, IMediator, Task, DashboardController, CancellationToken, Dictionary (+3 more)

### Community 42 - "Ticket Persistence Configs"
Cohesion: 0.16
Nodes (12): IEntityTypeConfiguration, DateTimeOffset, Guid, TicketAuditLog, Guid, TicketComment, ApprovalRequestConfiguration, ApprovalStepConfiguration (+4 more)

### Community 43 - "Telegram Webhook Controller"
Cohesion: 0.18
Nodes (10): CancellationToken, HttpPost, IActionResult, ILogger, Task, TelegramWebhookController, CancellationToken, Guid (+2 more)

### Community 44 - "AuditableEntity + SoftDelete Base"
Cohesion: 0.13
Nodes (13): DateTimeOffset, Guid, AuditableEntity, DateTimeOffset, Guid, ISoftDelete, DateTime, Guid (+5 more)

### Community 45 - "Leave Domain Entities"
Cohesion: 0.15
Nodes (11): Guid, LeaveBalance, DateTime, Guid, LeaveRequest, LeaveStatus, LeaveType, EntityTypeBuilder (+3 more)

### Community 46 - "Generic CRUD Controller"
Cohesion: 0.26
Nodes (9): Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, IMediator, Task (+1 more)

### Community 47 - "Authorized CRUD Controller"
Cohesion: 0.26
Nodes (10): Authorize, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, Task (+2 more)

### Community 48 - "Expense Claims API"
Cohesion: 0.14
Nodes (10): HttpGet, IActionResult, Task, ExpenseClaimsController, CancellationToken, IEnumerable, Task, Dictionary (+2 more)

### Community 49 - "Ticket Queries"
Cohesion: 0.30
Nodes (11): DateTimeOffset, Guid, List, GetTicketsByServiceDeskQuery, GetTicketsQuery, TicketDto, CancellationToken, IQueryable (+3 more)

### Community 50 - "Ticket Details Query"
Cohesion: 0.27
Nodes (11): CancellationToken, DateTimeOffset, Dictionary, Guid, List, Task, GetTicketDetailsQuery, GetTicketDetailsQueryHandler (+3 more)

### Community 51 - "Telegram Bot Service"
Cohesion: 0.42
Nodes (7): CancellationToken, Guid, ILogger, IServiceProvider, ITelegramBotClient, Task, TelegramGateway

### Community 52 - "Language + Theme Context (Frontend)"
Cohesion: 0.23
Nodes (9): LanguageContext, LanguageContextType, LanguageProvider(), AppTheme, ThemeContext, ThemeContextType, ThemeProvider(), useAppTheme() (+1 more)

### Community 53 - "Analytics API"
Cohesion: 0.17
Nodes (10): ControllerBase, HttpGet, IActionResult, Task, AnalyticsController, HttpGet, IActionResult, IMediator (+2 more)

### Community 54 - "Generic Authorized Controller"
Cohesion: 0.23
Nodes (9): Authorize, Guid, HttpGet, HttpPost, HttpPut, IActionResult, IMediator, Task (+1 more)

### Community 55 - "Graphify Skill Pipeline Steps"
Cohesion: 0.18
Nodes (11): graphify SKILL.md, Build Graph and Cluster Communities, Detect Files, Export Formats (Obsidian, Neo4j, Wiki, SVG), Generate Visualization, Label Communities, Manifest and Cost Tracking, Query / Path / Explain (+3 more)

### Community 56 - "API Client + Employee Directory (Frontend)"
Cohesion: 0.31
Nodes (5): RFC-7807, react, api, unwrapList(), ServiceDeskPortal()

### Community 57 - "Database Seeder"
Cohesion: 0.53
Nodes (5): IServiceProvider, OmniFlowDbContext, CancellationToken, Task, DatabaseSeeder

### Community 58 - "Auth Context (Frontend)"
Cohesion: 0.25
Nodes (7): AuthContext, AuthContextType, DecodedToken, useAuth(), User, ProtectedRoute(), LoginPage()

### Community 59 - "App Startup + SLA Sweep Service"
Cohesion: 0.20
Nodes (4): OmniFlow.Infrastructure.Persistence.Seed, OmniFlow.Infrastructure.Persistence, Guid, SeedConstants

### Community 60 - "Assets Application + API"
Cohesion: 0.24
Nodes (7): OmniFlow.Application.Assets.Commands, OmniFlow.Application.Assets.Queries, CancellationToken, Guid, Task, DeleteAssetCommand, DeleteAssetCommandHandler

### Community 61 - "MediatR Request + Tenant Context Interfaces"
Cohesion: 0.22
Nodes (9): IRequest, CancellationToken, Guid, IPublishEndpoint, ITenantService, IUserContext, Task, ApproveLeaveRequestCommand (+1 more)

### Community 62 - "Change Request Domain"
Cohesion: 0.20
Nodes (7): DateTime, Guid, ICollection, ChangeRequest, ChangeRisk, ChangeState, ChangeType

### Community 63 - "Configuration Item (CMDB) Domain"
Cohesion: 0.22
Nodes (7): Guid, ConfigurationItem, Location, CIStatus, CIType, EntityTypeBuilder, LocationConfiguration

### Community 64 - "Employment Domain Enums"
Cohesion: 0.22
Nodes (7): DateTime, Guid, EmploymentDetails, EmploymentStatus, EmploymentType, EntityTypeBuilder, EmploymentDetailsConfiguration

### Community 65 - "Leave Request Domain Events"
Cohesion: 0.29
Nodes (8): DateTime, Guid, LeaveRequestCreatedEvent, LeaveRequestResolvedEvent, ConsumeContext, ILogger, Task, LeaveRequestNotificationConsumer

### Community 66 - "Frontend App Entry Point"
Cohesion: 0.20
Nodes (10): omniflow-ui HTML Entry Point, main.tsx Script, #root div, Oxc, Oxlint Configuration (.oxlintrc.json), React Compiler, SWC, React + TypeScript + Vite Template (+2 more)

### Community 67 - "SLA Sweep Background Service"
Cohesion: 0.31
Nodes (7): BackgroundService, IServiceScopeFactory, CancellationToken, ILogger, Task, SlaSweepBackgroundService, TimeSpan

### Community 68 - "BaseEntity Domain Base Class"
Cohesion: 0.28
Nodes (5): IReadOnlyCollection, Guid, INotification, List, BaseEntity

### Community 69 - "Oxlint Config (Frontend)"
Cohesion: 0.22
Nodes (8): oxc, typescript, warn, plugins, rules, react/only-export-components, react/rules-of-hooks, $schema

### Community 70 - "Problem Management Domain"
Cohesion: 0.22
Nodes (8): Guid, ICollection, Problem, Guid, ICollection, List, ServiceDesk, ProblemState

### Community 71 - "Transition Ticket Command"
Cohesion: 0.29
Nodes (6): IRequestHandler, TransitionTicketCommand, CancellationToken, IMediator, Task, TransitionTicketCommandHandler

### Community 72 - "Assign Asset Command"
Cohesion: 0.32
Nodes (6): CancellationToken, Guid, IPublishEndpoint, Task, AssignAssetCommand, AssignAssetCommandHandler

### Community 73 - "Add Ticket Comment Command"
Cohesion: 0.32
Nodes (6): Guid, AddTicketCommentCommand, CancellationToken, Guid, Task, AddTicketCommentCommandHandler

### Community 74 - "Graphify Extraction Spec Docs"
Cohesion: 0.29
Nodes (7): Extraction Spec, EXTRACTED/INFERRED/AMBIGUOUS Classification, Confidence Score Rubric, Hyperedge Rules, Node ID Format, Semantic Similarity Edges, Vision Extraction Rules

### Community 75 - "Update Asset Command"
Cohesion: 0.38
Nodes (5): CancellationToken, Guid, Task, UpdateAssetCommand, UpdateAssetCommandHandler

### Community 76 - "Delete Department Command"
Cohesion: 0.38
Nodes (5): CancellationToken, Guid, Task, DeleteDepartmentCommand, DeleteDepartmentCommandHandler

### Community 77 - "Create KB Article Command"
Cohesion: 0.43
Nodes (6): CancellationToken, Dictionary, Guid, Task, CreateArticleCommand, CreateArticleCommandHandler

### Community 78 - "Create User Command"
Cohesion: 0.38
Nodes (6): Guid, CreateUserCommand, CancellationToken, Guid, Task, CreateUserCommandHandler

### Community 79 - "Asset Status + Persistence Config"
Cohesion: 0.33
Nodes (5): Guid, Asset, AssetStatus, EntityTypeBuilder, AssetConfiguration

### Community 80 - "Icon Sprite Sheet (Frontend Assets)"
Cohesion: 0.71
Nodes (7): icons.svg Sprite Sheet, Bluesky Icon, Discord Icon, Documentation Icon, GitHub Icon, Social (Contacts/Network) Icon, X (Twitter) Icon

### Community 81 - "Enterprise Data Grid (Frontend)"
Cohesion: 0.33
Nodes (5): EnterpriseDataGrid(), EnterpriseDataGridProps, GridFetchParams, GridToolbarAction, GroupableColumn

### Community 82 - "Tenant Entity + Config"
Cohesion: 0.40
Nodes (5): BaseEntity, DateTime, Tenant, EntityTypeBuilder, TenantConfiguration

### Community 83 - "Localization API"
Cohesion: 0.33
Nodes (4): Dictionary, HttpGet, IActionResult, LocalizationController

### Community 84 - "Employee Directory API"
Cohesion: 0.53
Nodes (4): HttpGet, IActionResult, Task, DirectoryController

### Community 85 - "Menu API"
Cohesion: 0.33
Nodes (5): HttpGet, IActionResult, IMediator, Task, MenuController

### Community 86 - "Create Asset Command"
Cohesion: 0.53
Nodes (5): CancellationToken, Guid, Task, CreateAssetCommand, CreateAssetCommandHandler

### Community 87 - "Create Department Command"
Cohesion: 0.53
Nodes (5): CancellationToken, Guid, Task, CreateDepartmentCommand, CreateDepartmentCommandHandler

### Community 88 - "Assign Ticket Command"
Cohesion: 0.40
Nodes (4): AssignTicketCommand, CancellationToken, Task, AssignTicketCommandHandler

### Community 89 - "Delete User Command"
Cohesion: 0.40
Nodes (5): CancellationToken, Guid, Task, DeleteUserCommand, DeleteUserCommandHandler

### Community 90 - "Update User Command"
Cohesion: 0.40
Nodes (5): CancellationToken, Guid, Task, UpdateUserCommand, UpdateUserCommandHandler

### Community 91 - "Employee Document Persistence Config"
Cohesion: 0.40
Nodes (5): DateTime, Guid, EmployeeDocument, EntityTypeBuilder, EmployeeDocumentConfiguration

### Community 92 - "Graphify Cross-repo Workflow Docs"
Cohesion: 0.50
Nodes (5): Cross-repo graph workflow, graphify clone <github-url>, graphify extract <path>, graphify merge-graphs, Monorepo / multi-service layout workflow

### Community 93 - "Auditable Entity Interceptor"
Cohesion: 0.40
Nodes (3): OmniFlow.Infrastructure.Persistence.Interceptors, EntityEntry, ChangeTrackerExtensions

### Community 94 - "Application DI Registration"
Cohesion: 0.40
Nodes (3): OmniFlow.Application, IServiceCollection, DependencyInjection

### Community 95 - "Dashboard Metrics Broadcast Service"
Cohesion: 0.40
Nodes (3): CancellationToken, Task, IDashboardMetricsService

### Community 96 - "HR Frontend Models + Service"
Cohesion: 0.60
Nodes (3): AssetDto, DepartmentDto, HrService

### Community 97 - "Graphify Skill Trigger Docs"
Cohesion: 0.50
Nodes (4): WerkPilot .claude/CLAUDE.md, graphify skill, graphify SKILL.md reference, /graphify trigger command

### Community 98 - "Graphify Watch Mode Docs"
Cohesion: 0.50
Nodes (4): --debounce flag, graphify.watch (--watch), needs_update flag, Token reduction benchmark

### Community 99 - "Graphify Export Formats Docs"
Cohesion: 0.50
Nodes (4): FalkorDB export (--falkordb / --falkordb-push), MCP server (--mcp), Neo4j export (--neo4j / --neo4j-push), Wiki export (--wiki)

### Community 100 - "Natural Sort Comparer Utility"
Cohesion: 0.50
Nodes (3): IComparer, NaturalComparer, NaturalComparer

### Community 101 - "Identity Seed Data"
Cohesion: 0.50
Nodes (3): IServiceProvider, Task, IdentitySeedData

### Community 102 - "React + Vite Scaffold Docs"
Cohesion: 0.67
Nodes (3): React (JavaScript Library), Vite (Build Tool Scaffold), React Logo (Vite Scaffold Asset)

### Community 104 - "Brand Assets Docs"
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
- **196 isolated node(s):** `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.2)`, `Microsoft.EntityFrameworkCore.Design (10.0.9)`, `OpenTelemetry.Exporter.OpenTelemetryProtocol (1.16.0)`, `OpenTelemetry.Extensions.Hosting (1.16.0)` (+191 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **11 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

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
- **Why does `IOmniFlowDbContext` connect `Core Domain Entities` to `EF Core Interceptors + API Actions`, `HR & Identity Domain Entities`, `Leave Requests + Audit Log Queries`, `Tenant Service + Leave Domain`, `Approvals Application + API`, `UI Schema API`, `Message Scheduler + Controllers`, `Localization + Controller Actions`, `Event Consumers + Messaging`, `Departments Application + API`, `Approval Validators`, `Tenant Service + Asset Events`, `Approval Domain Events`, `Knowledge Base Application + API`, `Menus Application + API`, `Generic Controller Actions`, `Authorized Controller Actions`, `Dashboard Application + API`, `Authorized CRUD Controller`, `Expense Claims API`, `Ticket Queries`, `Ticket Details Query`, `Analytics API`, `Assets Application + API`, `MediatR Request + Tenant Context Interfaces`, `Leave Request Domain Events`, `Transition Ticket Command`, `Assign Asset Command`, `Add Ticket Comment Command`, `Update Asset Command`, `Delete Department Command`, `Create KB Article Command`, `Create User Command`, `Tenant Entity + Config`, `Employee Directory API`, `Create Asset Command`, `Create Department Command`, `Assign Ticket Command`, `Delete User Command`, `Update User Command`?**
  _High betweenness centrality (0.172) - this node is a cross-community bridge._
- **Why does `OmniFlow.Application.Interfaces` connect `Event Consumers + Messaging` to `EF Core Interceptors + API Actions`, `SignalR Hubs + Health Checks`, `Auditable Entity + Domain Commands`, `Leave Requests + Audit Log Queries`, `ASP.NET Identity + Auth`, `Approvals Application + API`, `Approvals + Assets Domain`, `Departments Application + API`, `Exceptions + Tickets/Users Commands`, `Notification Gateway`, `Tenant Service + Asset Events`, `Tickets Queries + Grid Models`, `Knowledge Base Application + API`, `Menus Application + API`, `MediatR Pipeline Behaviors`, `Dashboard Application + API`, `Telegram Webhook Controller`, `Ticket Details Query`, `App Startup + SLA Sweep Service`, `Assets Application + API`, `Assign Asset Command`, `Update Asset Command`, `Delete Department Command`, `Auditable Entity Interceptor`, `Dashboard Metrics Broadcast Service`?**
  _High betweenness centrality (0.112) - this node is a cross-community bridge._
- **Why does `OmniFlow.Domain.Entities` connect `Auditable Entity + Domain Commands` to `Menus Application + API`, `Localization + Controller Actions`, `Approvals + Assets Domain`, `Ticket Details Query`, `Event Consumers + Messaging`, `Exceptions + Tickets/Users Commands`, `App Startup + SLA Sweep Service`, `Tickets Queries + Grid Models`?**
  _High betweenness centrality (0.099) - this node is a cross-community bridge._