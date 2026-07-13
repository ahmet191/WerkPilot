# Graph Report - src  (2026-07-13)

## Corpus Check
- 251 files · ~63,903 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 1499 nodes · 2876 edges · 74 communities (71 shown, 3 thin omitted)
- Extraction: 98% EXTRACTED · 2% INFERRED · 0% AMBIGUOUS · INFERRED: 53 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- Community 0
- Community 1
- Community 2
- Community 3
- Community 4
- Community 5
- Community 6
- Community 7
- Community 8
- Community 9
- Community 10
- Community 11
- Community 12
- Community 13
- Community 14
- Community 15
- Community 16
- Community 17
- Community 18
- Community 19
- Community 20
- Community 21
- Community 22
- Community 23
- Community 24
- Community 25
- Community 26
- Community 27
- Community 28
- Community 29
- Community 30
- Community 31
- Community 32
- Community 33
- Community 34
- Community 35
- Community 36
- Community 37
- Community 38
- Community 39
- Community 40
- Community 41
- Community 42
- Community 43
- Community 44
- Community 45
- Community 46
- Community 47
- Community 48
- Community 49
- Community 50
- Community 51
- Community 52
- Community 53
- Community 54
- Community 55
- Community 56
- Community 57
- Community 58
- Community 59
- Community 60
- Community 61
- Community 62
- Community 63
- Community 64
- Community 65
- Community 66
- Community 67
- Community 68
- Community 69
- Community 70
- Community 71
- Community 72

## God Nodes (most connected - your core abstractions)
1. `IOmniFlowDbContext` - 80 edges
2. `OmniFlow.Domain.Entities` - 76 edges
3. `OmniFlow.Application.Interfaces` - 67 edges
4. `OmniFlow.Domain.Enums` - 54 edges
5. `OmniFlow.Domain.Common` - 52 edges
6. `OmniFlowDbContext` - 49 edges
7. `AuditableEntity` - 40 edges
8. `User` - 32 edges
9. `Ticket` - 24 edges
10. `OmniFlow.Api.Controllers` - 22 edges

## Surprising Connections (you probably didn't know these)
- `AnalyticsController` --references--> `IOmniFlowDbContext`  [EXTRACTED]
  OmniFlow.Api/Controllers/AnalyticsController.cs → OmniFlow.Application/Interfaces/IOmniFlowDbContext.cs
- `CategoriesController` --references--> `IOmniFlowDbContext`  [EXTRACTED]
  OmniFlow.Api/Controllers/CategoriesController.cs → OmniFlow.Application/Interfaces/IOmniFlowDbContext.cs
- `CategoriesController` --references--> `IUserContext`  [EXTRACTED]
  OmniFlow.Api/Controllers/CategoriesController.cs → OmniFlow.Application/Interfaces/IUserContext.cs
- `DirectoryController` --references--> `IOmniFlowDbContext`  [EXTRACTED]
  OmniFlow.Api/Controllers/DirectoryController.cs → OmniFlow.Application/Interfaces/IOmniFlowDbContext.cs
- `ExpenseClaimsController` --references--> `IOmniFlowDbContext`  [EXTRACTED]
  OmniFlow.Api/Controllers/ExpenseClaimsController.cs → OmniFlow.Application/Interfaces/IOmniFlowDbContext.cs

## Import Cycles
- None detected.

## Communities (74 total, 3 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.05
Nodes (66): RFC-7807, api, unwrapList(), AuthContext, AuthContextType, AuthProvider(), DecodedToken, useAuth() (+58 more)

### Community 1 - "Community 1"
Cohesion: 0.05
Nodes (43): OmniFlow.Application.Tickets.EventHandlers, IHubContext, INotification, INotificationHandler, CancellationToken, HttpPost, IActionResult, ILogger (+35 more)

### Community 2 - "Community 2"
Cohesion: 0.09
Nodes (4): OmniFlow.Domain.Common, OmniFlow.Infrastructure.Persistence.Configurations, OmniFlow.Domain.Entities, OmniFlow.Domain.Enums

### Community 3 - "Community 3"
Cohesion: 0.05
Nodes (33): OmniFlow.Application.AuditLogs.Queries, Expression, HttpGet, IActionResult, IMediator, Task, AuditLogsController, HttpGet (+25 more)

### Community 4 - "Community 4"
Cohesion: 0.05
Nodes (43): @ant-design/icons, antd, axios, jwt-decode, lucide-react, @microsoft/signalr, dependencies, @ant-design/icons (+35 more)

### Community 5 - "Community 5"
Cohesion: 0.08
Nodes (31): Authorize, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, Task (+23 more)

### Community 6 - "Community 6"
Cohesion: 0.07
Nodes (28): ControllerBase, OmniFlow.Application.Dashboard.Queries, HttpGet, IActionResult, Task, AnalyticsController, HttpGet, IActionResult (+20 more)

### Community 7 - "Community 7"
Cohesion: 0.08
Nodes (27): OmniFlow.Application.Departments.Commands, OmniFlow.Application.Departments.Queries, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult (+19 more)

### Community 8 - "Community 8"
Cohesion: 0.08
Nodes (29): CancellationToken, Guid, IPublishEndpoint, Task, ApprovalRequestBuilder, CancellationToken, DateTime, Guid (+21 more)

### Community 9 - "Community 9"
Cohesion: 0.11
Nodes (9): OmniFlow.Application.Tickets.Queries, OmniFlow.Application.Interfaces, OmniFlow.Api.Controllers, OmniFlow.Application.Users.Commands, OmniFlow.Application.Common.Models, OmniFlow.Application.Common.Grid, OmniFlow.Application.Assets.Commands, Dictionary (+1 more)

### Community 10 - "Community 10"
Cohesion: 0.06
Nodes (25): OmniFlow.Api.Hubs, OmniFlow.Api.Services, HealthCheckContext, HealthCheckResult, Hub, IHealthCheck, IHostedService, Task (+17 more)

### Community 11 - "Community 11"
Cohesion: 0.06
Nodes (32): FluentValidation (12.1.1), FluentValidation.DependencyInjectionExtensions (12.1.1), MassTransit (8.3.4), MassTransit.Abstractions (8.3.4), MassTransit.EntityFrameworkCore (8.3.4), MediatR (14.2.0), MediatR.Contracts (2.0.1), Microsoft.AspNetCore.Identity.EntityFrameworkCore (10.0.3) (+24 more)

### Community 12 - "Community 12"
Cohesion: 0.07
Nodes (26): OmniFlow.Infrastructure.Persistence.Configurations.HR, OmniFlow.Domain.Entities.HR, DateTimeOffset, Guid, AuditableEntity, DateTimeOffset, Guid, ISoftDelete (+18 more)

### Community 13 - "Community 13"
Cohesion: 0.10
Nodes (27): OmniFlow.Application.Approvals.Queries, OmniFlow.Application.Approvals.Commands, Guid, HttpGet, HttpPost, IActionResult, IMediator, Task (+19 more)

### Community 14 - "Community 14"
Cohesion: 0.07
Nodes (17): OmniFlow.Infrastructure.Migrations, Migration, ModelSnapshot, MigrationBuilder, ModelBuilder, InitialBaseline, InitialBaseline, MigrationBuilder (+9 more)

### Community 15 - "Community 15"
Cohesion: 0.12
Nodes (23): ActionResult, Guid, HttpGet, HttpPost, IActionResult, IMediator, List, Task (+15 more)

### Community 16 - "Community 16"
Cohesion: 0.08
Nodes (22): AbstractValidator, IMessageScheduler, ApprovalStep, CancellationToken, Guid, IPublishEndpoint, Task, DecideApprovalStepCommand (+14 more)

### Community 17 - "Community 17"
Cohesion: 0.15
Nodes (14): Guid, HttpGet, IActionResult, Task, UiSchemaController, List, FieldOption, FormField (+6 more)

### Community 18 - "Community 18"
Cohesion: 0.10
Nodes (23): ActionResult, Authorize, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult (+15 more)

### Community 19 - "Community 19"
Cohesion: 0.11
Nodes (21): OmniFlow.Application.KnowledgeBase.Queries, OmniFlow.Application.KnowledgeBase.Commands, HttpGet, HttpPost, IActionResult, IMediator, Task, KnowledgeBaseController (+13 more)

### Community 20 - "Community 20"
Cohesion: 0.12
Nodes (19): Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult, IMediator, Task (+11 more)

### Community 21 - "Community 21"
Cohesion: 0.15
Nodes (18): ActionResult, Authorize, Guid, HttpDelete, HttpGet, HttpPost, HttpPut, IActionResult (+10 more)

### Community 22 - "Community 22"
Cohesion: 0.09
Nodes (10): OmniFlow.Api.Middleware, OmniFlow.Infrastructure.Persistence.Seed, OmniFlow.Infrastructure.Persistence.Interceptors, OmniFlow.Infrastructure.Messaging, OmniFlow.Infrastructure.Identity, OmniFlow.Infrastructure.Persistence, EntityEntry, ChangeTrackerExtensions (+2 more)

### Community 23 - "Community 23"
Cohesion: 0.08
Nodes (23): compilerOptions, allowArbitraryExtensions, allowImportingTsExtensions, erasableSyntaxOnly, jsx, lib, module, moduleDetection (+15 more)

### Community 24 - "Community 24"
Cohesion: 0.09
Nodes (16): DateTimeOffset, Dictionary, Guid, List, Ticket, Guid, TicketComment, TicketImpact (+8 more)

### Community 25 - "Community 25"
Cohesion: 0.10
Nodes (16): OmniFlow.Application.Common.Behaviors, OmniFlow.Application, IPipelineBehavior, IServiceCollection, CancellationToken, ILogger, int, RequestHandlerDelegate (+8 more)

### Community 26 - "Community 26"
Cohesion: 0.15
Nodes (15): ClaimsPrincipal, EnableRateLimiting, IdentityUser, IgnoreAntiforgeryToken, HttpGet, HttpPost, IActionResult, Task (+7 more)

### Community 27 - "Community 27"
Cohesion: 0.11
Nodes (16): Dictionary, MultiLingualString, DateTime, Guid, EmployeeDocument, Guid, KnowledgeBaseArticle, Guid (+8 more)

### Community 28 - "Community 28"
Cohesion: 0.13
Nodes (16): IEntityTypeConfiguration, Guid, ICollection, Department, Guid, SLAConfiguration, Guid, Team (+8 more)

### Community 29 - "Community 29"
Cohesion: 0.10
Nodes (19): compilerOptions, allowImportingTsExtensions, erasableSyntaxOnly, lib, module, moduleDetection, noEmit, noFallthroughCasesInSwitch (+11 more)

### Community 30 - "Community 30"
Cohesion: 0.11
Nodes (15): DateTime, Guid, ICollection, ChangeRequest, Guid, ICollection, Problem, Guid (+7 more)

### Community 31 - "Community 31"
Cohesion: 0.18
Nodes (13): OmniFlow.Application.Menus.Queries, HttpGet, IActionResult, IMediator, Task, MenuController, CancellationToken, Guid (+5 more)

### Community 32 - "Community 32"
Cohesion: 0.16
Nodes (9): OmniFlow.Application.Common.Exceptions, OmniFlow.Application.Tickets.Commands, Exception, IDictionary, ApiException, ConflictException, ForbiddenAccessException, NotFoundException (+1 more)

### Community 33 - "Community 33"
Cohesion: 0.13
Nodes (15): ASPNETCORE_ENVIRONMENT, applicationUrl, commandName, dotnetRunMessages, environmentVariables, launchBrowser, applicationUrl, commandName (+7 more)

### Community 34 - "Community 34"
Cohesion: 0.14
Nodes (11): DbContextOptionsBuilder, IdentityDbContext, IdentityRole, Guid, ApplicationRole, ApprovalRequest, ApprovalStep, DbSet (+3 more)

### Community 35 - "Community 35"
Cohesion: 0.16
Nodes (13): IRequest, IRequestHandler, CancellationToken, Guid, IPublishEndpoint, Task, AssignAssetCommand, AssignAssetCommandHandler (+5 more)

### Community 36 - "Community 36"
Cohesion: 0.14
Nodes (12): HttpContext, RequestDelegate, Task, AuditLoggingMiddleware, Guid, IUserContext, CancellationToken, Guid (+4 more)

### Community 37 - "Community 37"
Cohesion: 0.15
Nodes (11): Guid, LeaveBalance, DateTime, Guid, LeaveRequest, LeaveStatus, LeaveType, EntityTypeBuilder (+3 more)

### Community 38 - "Community 38"
Cohesion: 0.31
Nodes (8): CancellationToken, IServiceProvider, OmniFlowDbContext, Task, DatabaseSeeder, IServiceProvider, Task, IdentitySeedData

### Community 39 - "Community 39"
Cohesion: 0.18
Nodes (9): IReadOnlyCollection, Guid, INotification, List, BaseEntity, DateTime, Tenant, EntityTypeBuilder (+1 more)

### Community 40 - "Community 40"
Cohesion: 0.30
Nodes (11): DateTimeOffset, Guid, List, GetTicketsByServiceDeskQuery, GetTicketsQuery, TicketDto, CancellationToken, IQueryable (+3 more)

### Community 41 - "Community 41"
Cohesion: 0.22
Nodes (3): OmniFlow.Infrastructure.Messaging.Consumers, OmniFlow.Domain.Events, OmniFlow.Infrastructure.Notifications

### Community 42 - "Community 42"
Cohesion: 0.17
Nodes (10): DateTime, Guid, Compensation, DateTime, Guid, User, Gender, MaritalStatus (+2 more)

### Community 43 - "Community 43"
Cohesion: 0.24
Nodes (9): DateTime, Guid, AuditLogDto, CancellationToken, Task, GetAuditLogsQuery, GetAuditLogsQueryHandler, List (+1 more)

### Community 44 - "Community 44"
Cohesion: 0.18
Nodes (11): DbSet, IOmniFlowDbContext, DateTime, Guid, AuditLog, DateTime, Guid, AuthorityDelegation (+3 more)

### Community 45 - "Community 45"
Cohesion: 0.29
Nodes (9): ApprovalCompletedEvent, CancellationToken, ConsumeContext, ILogger, IMediator, IPublishEndpoint, string, Task (+1 more)

### Community 46 - "Community 46"
Cohesion: 0.20
Nodes (5): OmniFlow.Domain.Entities.Approvals, OmniFlow.Application.LeaveRequests.Commands, OmniFlow.Application.Approvals, string, ApprovalEntityTypes

### Community 47 - "Community 47"
Cohesion: 0.31
Nodes (8): OmniFlow.Application.LeaveRequests.Queries, CancellationToken, DateTime, Guid, Task, GetMyLeaveRequestsQuery, GetMyLeaveRequestsQueryHandler, LeaveRequestDto

### Community 48 - "Community 48"
Cohesion: 0.29
Nodes (7): DbContext, DbContextEventData, InterceptionResult, CancellationToken, AuditableEntityInterceptor, SaveChangesInterceptor, ValueTask

### Community 49 - "Community 49"
Cohesion: 0.22
Nodes (7): Guid, ConfigurationItem, Location, CIStatus, CIType, EntityTypeBuilder, LocationConfiguration

### Community 50 - "Community 50"
Cohesion: 0.22
Nodes (7): DateTime, Guid, EmploymentDetails, EmploymentStatus, EmploymentType, EntityTypeBuilder, EmploymentDetailsConfiguration

### Community 51 - "Community 51"
Cohesion: 0.29
Nodes (8): DateTime, Guid, LeaveRequestCreatedEvent, LeaveRequestResolvedEvent, ConsumeContext, ILogger, Task, LeaveRequestNotificationConsumer

### Community 52 - "Community 52"
Cohesion: 0.29
Nodes (7): CancellationToken, Guid, IConfiguration, ILogger, IServiceProvider, Task, TelegramIdentityService

### Community 53 - "Community 53"
Cohesion: 0.31
Nodes (7): BackgroundService, IServiceScopeFactory, CancellationToken, ILogger, Task, SlaSweepBackgroundService, TimeSpan

### Community 54 - "Community 54"
Cohesion: 0.36
Nodes (7): OmniFlow.Application.Assets.Queries, CancellationToken, Guid, Task, AssetDto, GetAssetsQuery, GetAssetsQueryHandler

### Community 55 - "Community 55"
Cohesion: 0.25
Nodes (7): IConsumer, Guid, ApprovalStepActivatedEvent, ConsumeContext, ILogger, Task, ApprovalStepActivatedConsumer

### Community 56 - "Community 56"
Cohesion: 0.31
Nodes (7): JsonSerializerOptions, Exception, HttpContext, ILogger, RequestDelegate, Task, ExceptionHandlingMiddleware

### Community 57 - "Community 57"
Cohesion: 0.25
Nodes (7): DateTime, Guid, AssetAssignedEvent, ConsumeContext, ILogger, Task, AssetAssignedConsumer

### Community 58 - "Community 58"
Cohesion: 0.25
Nodes (7): DateTime, Guid, CheckTicketSlaBreachEvent, ConsumeContext, ILogger, Task, TicketSlaBreachConsumer

### Community 59 - "Community 59"
Cohesion: 0.22
Nodes (8): plugins, rules, react/only-export-components, react/rules-of-hooks, $schema, oxc, typescript, warn

### Community 60 - "Community 60"
Cohesion: 0.32
Nodes (6): HttpContext, ILogger, RequestDelegate, string, Task, CorrelationIdMiddleware

### Community 61 - "Community 61"
Cohesion: 0.43
Nodes (6): CancellationToken, Guid, Task, DepartmentDto, GetDepartmentsQuery, GetDepartmentsQueryHandler

### Community 62 - "Community 62"
Cohesion: 0.32
Nodes (6): Guid, AddTicketCommentCommand, CancellationToken, Guid, Task, AddTicketCommentCommandHandler

### Community 63 - "Community 63"
Cohesion: 0.33
Nodes (5): Guid, ICollection, AppMenu, EntityTypeBuilder, AppMenuConfiguration

### Community 64 - "Community 64"
Cohesion: 0.33
Nodes (5): Guid, Asset, AssetStatus, EntityTypeBuilder, AssetConfiguration

### Community 65 - "Community 65"
Cohesion: 0.29
Nodes (5): DateTime, Guid, ExpenseClaim, ExpenseCategory, ExpenseStatus

### Community 66 - "Community 66"
Cohesion: 0.40
Nodes (5): CancellationToken, Guid, Task, DeleteAssetCommand, DeleteAssetCommandHandler

### Community 67 - "Community 67"
Cohesion: 0.40
Nodes (4): AssignTicketCommand, CancellationToken, Task, AssignTicketCommandHandler

### Community 68 - "Community 68"
Cohesion: 0.40
Nodes (5): DateTimeOffset, Guid, TicketAuditLog, EntityTypeBuilder, TicketAuditLogConfiguration

### Community 69 - "Community 69"
Cohesion: 0.50
Nodes (3): IComparer, NaturalComparer, NaturalComparer

## Knowledge Gaps
- **135 isolated node(s):** `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.2)`, `Microsoft.EntityFrameworkCore.Design (10.0.9)`, `OpenTelemetry.Exporter.OpenTelemetryProtocol (1.16.0)`, `OpenTelemetry.Extensions.Hosting (1.16.0)` (+130 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **3 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `IOmniFlowDbContext` connect `Community 44` to `Community 3`, `Community 5`, `Community 6`, `Community 7`, `Community 8`, `Community 13`, `Community 15`, `Community 16`, `Community 17`, `Community 18`, `Community 19`, `Community 20`, `Community 21`, `Community 24`, `Community 27`, `Community 28`, `Community 30`, `Community 31`, `Community 34`, `Community 35`, `Community 36`, `Community 37`, `Community 39`, `Community 40`, `Community 42`, `Community 43`, `Community 45`, `Community 46`, `Community 47`, `Community 49`, `Community 50`, `Community 51`, `Community 54`, `Community 55`, `Community 57`, `Community 58`, `Community 61`, `Community 62`, `Community 63`, `Community 64`, `Community 65`, `Community 66`, `Community 67`, `Community 68`?**
  _High betweenness centrality (0.215) - this node is a cross-community bridge._
- **Why does `OmniFlow.Application.Interfaces` connect `Community 9` to `Community 32`, `Community 1`, `Community 36`, `Community 6`, `Community 7`, `Community 70`, `Community 41`, `Community 10`, `Community 13`, `Community 46`, `Community 47`, `Community 19`, `Community 22`, `Community 54`, `Community 25`, `Community 61`, `Community 31`?**
  _High betweenness centrality (0.133) - this node is a cross-community bridge._
- **Why does `OmniFlow.Domain.Entities` connect `Community 2` to `Community 32`, `Community 9`, `Community 41`, `Community 46`, `Community 21`, `Community 22`, `Community 63`, `Community 31`?**
  _High betweenness centrality (0.081) - this node is a cross-community bridge._
- **What connects `net10.0`, `Microsoft.AspNetCore.OpenApi (10.0.2)`, `Microsoft.EntityFrameworkCore.Design (10.0.9)` to the rest of the system?**
  _135 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 0` be split into smaller, more focused modules?**
  _Cohesion score 0.054945054945054944 - nodes in this community are weakly interconnected._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.05406746031746032 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.08734693877551021 - nodes in this community are weakly interconnected._