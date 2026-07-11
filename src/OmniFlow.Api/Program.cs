using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Identity;
using OmniFlow.Infrastructure.Messaging;
using OmniFlow.Infrastructure.Persistence;
using OmniFlow.Infrastructure.Persistence.Interceptors;
using OmniFlow.Infrastructure.Persistence.Seed;
using OpenIddict.Abstractions;
using OpenIddict.Validation.AspNetCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using OmniFlow.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════
// 1. Serilog Setup
// ═══════════════════════════════════════════════════════
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// ═══════════════════════════════════════════════════════
// 2. OpenTelemetry Setup
// ═══════════════════════════════════════════════════════
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("OmniFlow.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// ═══════════════════════════════════════════════════════
// 3. Application Layer Setup
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<ITenantService, OmniFlow.Api.Services.TenantService>();

// Telegram Bot Client Setup
var telegramToken = builder.Configuration["Telegram:BotToken"];
if (!string.IsNullOrEmpty(telegramToken))
{
    builder.Services.AddSingleton<Telegram.Bot.ITelegramBotClient>(new Telegram.Bot.TelegramBotClient(telegramToken));
    builder.Services.AddHostedService<OmniFlow.Api.Services.TelegramWebhookRegistrationService>();
}

// ═══════════════════════════════════════════════════════
// 4. Infrastructure Layer Setup (DbContext + Identity + OpenIddict)
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<AuditableEntityInterceptor>();
builder.Services.AddDbContext<IOmniFlowDbContext, OmniFlowDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Host=localhost;Database=OmniFlowDb;Username=postgres;Password=postgres");

    // Register the OpenIddict entity sets in the DbContext
    options.UseOpenIddict<Guid>();
});

// ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<OmniFlowDbContext>()
.AddDefaultTokenProviders();

// OpenIddict
builder.Services.AddOpenIddict()
    // Core: use EF Core stores
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
               .UseDbContext<OmniFlowDbContext>()
               .ReplaceDefaultEntities<Guid>();
    })
    // Server: configure the authorization server
    .AddServer(options =>
    {
        options.SetTokenEndpointUris("/connect/token")
               .SetUserInfoEndpointUris("/connect/userinfo");

        options.AllowPasswordFlow()
               .AllowRefreshTokenFlow();

        // Register scopes
        options.RegisterScopes(
            OpenIddictConstants.Scopes.OpenId,
            OpenIddictConstants.Scopes.Email,
            OpenIddictConstants.Scopes.Profile,
            OpenIddictConstants.Scopes.Roles,
            OpenIddictConstants.Scopes.OfflineAccess);

        // Accept anonymous clients (no client_id/secret required from SPA)
        options.AcceptAnonymousClients();

        // Register ASP.NET Core host and enable passthrough for the token endpoint
        options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough()
               .DisableTransportSecurityRequirement(); // Allow HTTP in development

        // Use ephemeral keys for development (replace with persistent keys in production)
        options.AddEphemeralEncryptionKey()
               .AddEphemeralSigningKey();

        // Disable access token encryption so it's a plain JWT readable by APIs
        options.DisableAccessTokenEncryption();

        // Token lifetimes
        options.SetAccessTokenLifetime(TimeSpan.FromHours(1));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(30));
    })
    // Validation: validate tokens locally
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

// Override default authentication scheme to use OpenIddict validation
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Administrator", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("HRAdmin", policy => policy.RequireRole("HRAdmin", "Administrator"));
    options.AddPolicy("ITAgent", policy => policy.RequireRole("ITAgent", "Administrator"));
    options.AddPolicy("Requester", policy => policy.RequireRole("Requester", "Administrator", "HRAdmin", "ITAgent"));
});

// ═══════════════════════════════════════════════════════
// 5. Infrastructure Layer Setup (Services)
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<IMessageGateway, TelegramGateway>();
builder.Services.AddScoped<ITelegramIdentityService, TelegramIdentityService>();
builder.Services.AddScoped<IDashboardMetricsService, OmniFlow.Api.Services.DashboardMetricsService>();

// ═══════════════════════════════════════════════════════
// 6. MassTransit (Event-Driven Design)
// ═══════════════════════════════════════════════════════
builder.Services.AddMassTransit(x =>
{
    x.AddDelayedMessageScheduler();

    // Register Consumers
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.LeaveRequestNotificationConsumer>();
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.TicketSlaBreachConsumer>();
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.AssetAssignedConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.UseDelayedMessageScheduler();
        cfg.ConfigureEndpoints(context);
    });
});

// ═══════════════════════════════════════════════════════
// 7. ASP.NET Core Services
// ═══════════════════════════════════════════════════════
builder.Services.AddControllers()
    .AddJsonOptions(options => 
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, OmniFlow.Api.Services.UserContextService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteUI", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ═══════════════════════════════════════════════════════
// BUILD APP
// ═══════════════════════════════════════════════════════
var app = builder.Build();

// ═══════════════════════════════════════════════════════
// Middleware Pipeline
// ═══════════════════════════════════════════════════════
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<OmniFlow.Api.Middlewares.ExceptionHandlingMiddleware>();
app.UseMiddleware<OmniFlow.Api.Middleware.AuditLoggingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowViteUI");

// Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<OmniFlow.Api.Hubs.DashboardHub>("/hubs/dashboard");

// ═══════════════════════════════════════════════════════
// Database Initialization & Seeding
// ═══════════════════════════════════════════════════════
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IOmniFlowDbContext>() as DbContext;
    if (context != null)
    {
        try
        {
            // Drop and recreate DB schema to apply new tables
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            // ── Identity & OpenIddict Seed ──
            await IdentitySeedData.SeedAsync(scope.ServiceProvider);

            // ── 1. Seed Legacy Users representing different roles ──
            if (!await context.Set<OmniFlow.Domain.Entities.User>().AnyAsync())
            {
                var departmentId = Guid.Parse("55555555-5555-5555-5555-555555555555");
                var department = new OmniFlow.Domain.Entities.Department
                {
                    Id = departmentId,
                    LocalizedName = new OmniFlow.Domain.Common.MultiLingualString { Translations = new Dictionary<string, string> { { "en", "HQ" }, { "tr", "Merkez" } } }
                };
                context.Set<OmniFlow.Domain.Entities.Department>().Add(department);

                var requester = new OmniFlow.Domain.Entities.User
                {
                    Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = "Alice Requester",
                    Email = "alice@omniflow.com",
                    Role = OmniFlow.Domain.Enums.UserRole.Requester,
                    DepartmentId = departmentId,
                    TelegramChatId = "8920226584"
                };

                var agent = new OmniFlow.Domain.Entities.User
                {
                    Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    Name = "Bob Agent",
                    Email = "bob@omniflow.com",
                    Role = OmniFlow.Domain.Enums.UserRole.Agent,
                    DepartmentId = departmentId
                };

                var approver = new OmniFlow.Domain.Entities.User
                {
                    Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    Name = "Charlie Approver",
                    Email = "charlie@omniflow.com",
                    Role = OmniFlow.Domain.Enums.UserRole.Approver,
                    DepartmentId = departmentId
                };

                var admin = new OmniFlow.Domain.Entities.User
                {
                    Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
                    Name = "Dave Admin",
                    Email = "dave@omniflow.com",
                    Role = OmniFlow.Domain.Enums.UserRole.Administrator,
                    DepartmentId = departmentId
                };

                context.Set<OmniFlow.Domain.Entities.User>().AddRange(requester, agent, approver, admin);
                await context.SaveChangesAsync();
            }

            // Create default ServiceDesk first
            var serviceDeskId = Guid.Parse("77777777-7777-7777-7777-777777777777");
            if (!await context.Set<OmniFlow.Domain.Entities.ServiceDesk>().AnyAsync())
            {
                var serviceDesk = new OmniFlow.Domain.Entities.ServiceDesk
                {
                    Id = serviceDeskId,
                    Name = "IT Service Desk",
                    Description = "Global IT Support",
                    DepartmentId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    IsPublicToAllEmployees = true
                };
                context.Set<OmniFlow.Domain.Entities.ServiceDesk>().Add(serviceDesk);
                await context.SaveChangesAsync();
            }

            // ── 2. Seed IT Support Request Template ──
            if (!await context.Set<OmniFlow.Domain.Entities.TicketTemplate>().AnyAsync())
            {
                var template = new OmniFlow.Domain.Entities.TicketTemplate
                {
                    Id = Guid.Parse("2e3e5c9b-6dbb-4171-aa7a-8cf0c74fb1d2"),
                    LocalizedName = new OmniFlow.Domain.Common.MultiLingualString { Translations = new Dictionary<string, string> { { "en", "IT Support Request" }, { "tr", "BT Destek Talebi" } } },
                    ServiceDeskId = serviceDeskId,
                    Fields = new List<OmniFlow.Domain.Entities.FieldDefinition>
                    {
                        new() { Key = "Title", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Title"), Type = "Text", IsRequired = true, ValidationRegex = "^.{5,150}$", LocalizedDescription = new OmniFlow.Domain.Common.MultiLingualString("en", "Short description of the issue") },
                        new() { Key = "Type", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Type"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Incident", "ServiceRequest", "Problem", "Change" } },
                        new() { Key = "Priority", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Priority"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High", "Critical" }, LocalizedDescription = new OmniFlow.Domain.Common.MultiLingualString("en", "Urgency levels") },
                        new() { Key = "Impact", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Impact"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High" } },
                        new() { Key = "Urgency", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Urgency"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High" } },
                        new() { Key = "Description", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Description"), Type = "Text", IsRequired = false, LocalizedDescription = new OmniFlow.Domain.Common.MultiLingualString("en", "Detailed logs or explanation") },
                        new() { Key = "AssetTag", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Asset Tag"), Type = "Text", IsRequired = false, ValidationRegex = "^[A-Z]{3}-\\\\d{4}$", LocalizedDescription = new OmniFlow.Domain.Common.MultiLingualString("en", "Device asset tag in ABC-1234 format") }
                    },
                    Workflow = new OmniFlow.Domain.Entities.WorkflowDefinition
                    {
                        Transitions = new List<OmniFlow.Domain.Entities.WorkflowTransition>
                        {
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.New, ToState = OmniFlow.Domain.Enums.TicketState.InProgress, Trigger = "StartWork", RequiredRole = OmniFlow.Domain.Enums.UserRole.Agent },
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.InProgress, ToState = OmniFlow.Domain.Enums.TicketState.WaitingForApproval, Trigger = "RequestApproval", RequiredRole = OmniFlow.Domain.Enums.UserRole.Agent },
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.WaitingForApproval, ToState = OmniFlow.Domain.Enums.TicketState.Resolved, Trigger = "Approve", RequiredRole = OmniFlow.Domain.Enums.UserRole.Approver },
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.WaitingForApproval, ToState = OmniFlow.Domain.Enums.TicketState.InProgress, Trigger = "Reject", RequiredRole = OmniFlow.Domain.Enums.UserRole.Approver },
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.Resolved, ToState = OmniFlow.Domain.Enums.TicketState.Closed, Trigger = "Close", RequiredRole = OmniFlow.Domain.Enums.UserRole.Requester }
                        }
                    }
                };

                context.Set<OmniFlow.Domain.Entities.TicketTemplate>().Add(template);
                await context.SaveChangesAsync();
            }

            // ── 3. Seed Dynamic Menu Hierarchy ──
            if (!await context.Set<OmniFlow.Domain.Entities.AppMenu>().AnyAsync())
            {
                var dashboard = new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), TitleKey = "sidebar.dashboard", Path = "/", Icon = "DashboardOutlined", OrderIndex = 10 };
                var itsmParent = new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), TitleKey = "sidebar.itsm", Icon = "AppstoreOutlined", OrderIndex = 20 };
                var hrParent = new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), TitleKey = "sidebar.hr", Icon = "TeamOutlined", OrderIndex = 30 };
                var adminParent = new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), TitleKey = "sidebar.admin", Icon = "SettingOutlined", OrderIndex = 40 };

                var menus = new List<OmniFlow.Domain.Entities.AppMenu>
                {
                    dashboard,
                    itsmParent,
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = itsmParent.Id, TitleKey = "sidebar.servicedeskportal", Path = "/servicedeskportal", Icon = "AppstoreOutlined", OrderIndex = 10 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = itsmParent.Id, TitleKey = "sidebar.tickets", Path = "/tickets", Icon = "ContainerOutlined", OrderIndex = 20 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = itsmParent.Id, TitleKey = "sidebar.assets", Path = "/assets", Icon = "LaptopOutlined", OrderIndex = 30 },
                    
                    hrParent,
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = hrParent.Id, TitleKey = "sidebar.directory", Path = "/directory", Icon = "TeamOutlined", OrderIndex = 10 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = hrParent.Id, TitleKey = "sidebar.orgchart", Path = "/orgchart", Icon = "DashboardOutlined", OrderIndex = 20 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = hrParent.Id, TitleKey = "sidebar.leaves", Path = "/leaves", Icon = "CalendarOutlined", OrderIndex = 30 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = hrParent.Id, TitleKey = "sidebar.expenses", Path = "/expenses", Icon = "DollarOutlined", OrderIndex = 40 },
                    
                    adminParent,
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.departments", Path = "/departments", Icon = "TeamOutlined", OrderIndex = 10 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.servicedesks", Path = "/servicedesks", Icon = "ContainerOutlined", OrderIndex = 20 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.auditlogs", Path = "/auditlogs", Icon = "SafetyCertificateOutlined", OrderIndex = 30 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.users", Path = "/users", Icon = "UserOutlined", OrderIndex = 40 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.templates", Path = "/templates", Icon = "FileTextOutlined", OrderIndex = 50 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.categories", Path = "/categories", Icon = "AppstoreAddOutlined", OrderIndex = 60 },
                    new OmniFlow.Domain.Entities.AppMenu { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.slas", Path = "/slas", Icon = "FieldTimeOutlined", OrderIndex = 70 }
                };

                context.Set<OmniFlow.Domain.Entities.AppMenu>().AddRange(menus);
                await context.SaveChangesAsync();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database initialization failed: {ex}");
        }
    }
}

app.Run();
