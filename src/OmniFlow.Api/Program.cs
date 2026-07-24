using MassTransit;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application;
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
using Quartz;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using System.Threading.RateLimiting;
using OmniFlow.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════
// 1. Serilog Setup
// ═══════════════════════════════════════════════════════
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Application", "OmniFlow.Api")
    .WriteTo.Console());

// ═══════════════════════════════════════════════════════
// 2. OpenTelemetry Setup
// ═══════════════════════════════════════════════════════
// The OTLP exporter is only attached when Otlp:Endpoint is configured, so the
// API boots fine without a local collector. Point it at an OTel collector /
// Jaeger / Grafana Alloy to ship traces and metrics.
var otlpEndpoint = builder.Configuration["Otlp:Endpoint"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("OmniFlow.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddEntityFrameworkCoreInstrumentation();

        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            // Always expose a Prometheus scrape endpoint (/metrics) — it's pull-based
            // and needs no collector, so metrics are available even without OTLP.
            .AddPrometheusExporter();

        if (!string.IsNullOrEmpty(otlpEndpoint))
        {
            metrics.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
    });

// ═══════════════════════════════════════════════════════
// 3. Application Layer Setup
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<ITenantService, OmniFlow.Api.Services.TenantService>();

// MediatR handlers + FluentValidation validators + logging/validation pipeline behaviors.
builder.Services.AddApplication();

// Notification handlers living in Infrastructure (Telegram/SignalR reactions to
// domain events) must be scanned separately — they are not in the Application assembly.
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(OmniFlow.Infrastructure.Messaging.TelegramGateway).Assembly));

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

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not configured. Set it via appsettings.Development.json, user-secrets, or environment variables.");
}

builder.Services.AddDbContext<IOmniFlowDbContext, OmniFlowDbContext>(options =>
{
    options.UseNpgsql(connectionString);

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

// ── External identity: LDAP (optional), corporate OIDC SSO (optional) ──
builder.Services.Configure<OmniFlow.Infrastructure.Identity.Ldap.LdapOptions>(
    builder.Configuration.GetSection("Ldap"));
builder.Services.AddScoped<OmniFlow.Infrastructure.Identity.Ldap.ILdapAuthenticator,
    OmniFlow.Infrastructure.Identity.Ldap.LdapAuthenticator>();
builder.Services.AddScoped<OmniFlow.Infrastructure.Identity.ExternalUserProvisioner>();
builder.Services.AddScoped<OmniFlow.Infrastructure.Identity.SsoTicketService>();

// Outbound email + login-account lifecycle (admin console, password recovery).
builder.Services.Configure<OmniFlow.Infrastructure.Email.EmailOptions>(
    builder.Configuration.GetSection("Email"));
builder.Services.AddScoped<OmniFlow.Application.Interfaces.IEmailSender,
    OmniFlow.Infrastructure.Email.SmtpEmailSender>();
builder.Services.AddSingleton<OmniFlow.Application.Interfaces.ICronScheduleService,
    OmniFlow.Infrastructure.Scheduling.CronScheduleService>();
builder.Services.AddScoped<OmniFlow.Application.Interfaces.IIdentityAccountService,
    OmniFlow.Infrastructure.Identity.IdentityAccountService>();

// Phase 5 AI Service Registration
builder.Services.AddHttpClient<OmniFlow.Application.Interfaces.IAiTriageService, 
    OmniFlow.Infrastructure.Services.AiTriageService>();

// Per-tenant permission matrix: endpoints are gated by permission keys that a
// tenant admin can re-map to roles at runtime (see PermissionsController).
builder.Services.AddMemoryCache();
builder.Services.AddScoped<OmniFlow.Application.Interfaces.IPermissionService,
    OmniFlow.Infrastructure.Authorization.PermissionService>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider,
    OmniFlow.Infrastructure.Authorization.PermissionPolicyProvider>();
builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    OmniFlow.Infrastructure.Authorization.PermissionAuthorizationHandler>();

// Tenant lifecycle (platform operators).
builder.Services.AddScoped<OmniFlow.Application.Interfaces.ITenantProvisioningService,
    OmniFlow.Infrastructure.Identity.TenantProvisioningService>();

// Notification centre: in-app records fanned out to email/Telegram/SignalR.
builder.Services.AddScoped<OmniFlow.Application.Interfaces.INotificationPusher,
    OmniFlow.Api.Hubs.SignalRNotificationPusher>();
builder.Services.AddScoped<OmniFlow.Application.Interfaces.INotificationService,
    OmniFlow.Infrastructure.Notifications.NotificationService>();

// Tenant-customizable UI strings: shipped defaults merged with DB overrides.
builder.Services.AddScoped<OmniFlow.Application.Interfaces.ITranslationService,
    OmniFlow.Infrastructure.Localization.TranslationService>();

// Tenant-customizable UI schemas: shipped built-in schemas merged with DB overrides.
builder.Services.AddScoped<OmniFlow.Application.Interfaces.IUiSchemaService,
    OmniFlow.Infrastructure.UiSchemas.UiSchemaService>();

// Ticket attachments: metadata in the DB, bytes in a config-selected blob store.
// Local disk by default (needs a mounted volume to survive redeploys); S3-compatible
// (AWS/MinIO) for portable durable storage. Storage:Provider = Local | S3.
var storageProvider = builder.Configuration["Storage:Provider"] ?? "Local";
if (storageProvider.Equals("S3", StringComparison.OrdinalIgnoreCase))
{
    var s3Options = builder.Configuration.GetSection("Storage:S3")
        .Get<OmniFlow.Infrastructure.Storage.S3StorageOptions>()
        ?? new OmniFlow.Infrastructure.Storage.S3StorageOptions();
    builder.Services.AddSingleton(s3Options);
    builder.Services.AddSingleton<Amazon.S3.IAmazonS3>(_ =>
    {
        var config = new Amazon.S3.AmazonS3Config { ForcePathStyle = s3Options.ForcePathStyle };
        // A ServiceUrl points at MinIO/self-hosted; its absence means real AWS by Region.
        if (!string.IsNullOrWhiteSpace(s3Options.ServiceUrl))
            config.ServiceURL = s3Options.ServiceUrl;
        else
            config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(s3Options.Region);
        // Explicit keys for MinIO; absent keys fall back to the AWS credential chain.
        return string.IsNullOrWhiteSpace(s3Options.AccessKey)
            ? new Amazon.S3.AmazonS3Client(config)
            : new Amazon.S3.AmazonS3Client(
                new Amazon.Runtime.BasicAWSCredentials(s3Options.AccessKey, s3Options.SecretKey), config);
    });
    builder.Services.AddSingleton<OmniFlow.Application.Interfaces.IFileStorage,
        OmniFlow.Infrastructure.Storage.S3FileStorage>();
}
else
{
    builder.Services.AddSingleton<OmniFlow.Application.Interfaces.IFileStorage,
        OmniFlow.Infrastructure.Storage.LocalFileStorage>();
}

// Short-lived cookie that only carries the external IdP round-trip; the SPA
// never sees it — it exchanges the SSO ticket for real tokens instead.
builder.Services.AddAuthentication()
    .AddCookie(OmniFlow.Api.Controllers.SsoController.ExternalCookieScheme, options =>
    {
        options.Cookie.Name = "WerkPilot.External";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    });

var oidcConfig = builder.Configuration.GetSection("Sso:Oidc");
if (oidcConfig.GetValue<bool>("Enabled") && !string.IsNullOrWhiteSpace(oidcConfig["Authority"]))
{
    builder.Services.AddAuthentication()
        .AddOpenIdConnect(OmniFlow.Api.Controllers.SsoController.OidcScheme,
            oidcConfig["DisplayName"] ?? "Corporate SSO",
            options =>
            {
                options.SignInScheme = OmniFlow.Api.Controllers.SsoController.ExternalCookieScheme;
                options.Authority = oidcConfig["Authority"];
                options.ClientId = oidcConfig["ClientId"];
                options.ClientSecret = oidcConfig["ClientSecret"];
                options.ResponseType = "code";
                options.UsePkce = true;
                options.CallbackPath = "/signin-oidc-external";
                options.GetClaimsFromUserInfoEndpoint = true;
                options.SaveTokens = false;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
            });
}

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
               .AllowRefreshTokenFlow()
               // SPA redeems one-time SSO exchange tickets minted by the
               // external-IdP callback (see SsoController / AuthorizationController).
               .AllowCustomFlow(OmniFlow.Api.Controllers.AuthorizationController.SsoGrantType);

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
        var aspNetCoreBuilder = options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough();

        if (builder.Environment.IsDevelopment())
        {
            // Allow HTTP locally only.
            aspNetCoreBuilder.DisableTransportSecurityRequirement();

            // Encryption: use an in-memory SYMMETRIC key, NOT a development
            // certificate. OpenIddict encrypts refresh tokens; validating a
            // refresh token then DECRYPTS it, which with a cert requires the
            // private key from the OS key store. On macOS that private-key
            // access hangs a headless dev server, so the refresh_token grant
            // (and thus silent token refresh) times out. A symmetric key lives
            // in process memory — no keychain, no hang — and is stable across
            // restarts so issued tokens keep validating. Dev-only literal key.
            options.AddEncryptionKey(new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes("omniflow-dev-encryption-key-32by")));

            // Signing uses the public/private cert too, but only the signing
            // (creation) path runs in dev and it works fine; keep it so access
            // tokens remain standard RS256 JWTs.
            options.AddDevelopmentSigningCertificate();
        }
        else
        {
            var signingCertPath = builder.Configuration["OpenIddict:SigningCertificate:Path"];
            var encryptionCertPath = builder.Configuration["OpenIddict:EncryptionCertificate:Path"];
            if (string.IsNullOrEmpty(signingCertPath) || string.IsNullOrEmpty(encryptionCertPath))
            {
                throw new InvalidOperationException(
                    "Production requires persistent certificates. Configure OpenIddict:SigningCertificate and OpenIddict:EncryptionCertificate (Path + Password).");
            }

            options.AddSigningCertificate(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
                signingCertPath, builder.Configuration["OpenIddict:SigningCertificate:Password"]));
            options.AddEncryptionCertificate(System.Security.Cryptography.X509Certificates.X509CertificateLoader.LoadPkcs12FromFile(
                encryptionCertPath, builder.Configuration["OpenIddict:EncryptionCertificate:Password"]));
        }

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

    // Platform operations (tenant provisioning) sit above the tenant boundary,
    // so they are gated on a plain Identity role rather than the matrix.
    options.AddPolicy(OmniFlow.Domain.Common.PlatformRoles.PlatformAdminPolicy,
        policy => policy.RequireRole(OmniFlow.Domain.Common.PlatformRoles.PlatformAdmin));

    // Secure by default: every endpoint requires an authenticated user unless
    // it explicitly opts out with [AllowAnonymous].
    options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ═══════════════════════════════════════════════════════
// 5. Infrastructure Layer Setup (Services)
// ═══════════════════════════════════════════════════════
builder.Services.AddScoped<IMessageGateway, TelegramGateway>();
builder.Services.AddScoped<ITelegramIdentityService, TelegramIdentityService>();
builder.Services.AddScoped<IDashboardMetricsService, OmniFlow.Api.Services.DashboardMetricsService>();

// ═══════════════════════════════════════════════════════
// 6. Quartz (Durable Scheduling)
// ═══════════════════════════════════════════════════════
// Persistent job store in PostgreSQL: scheduled work (exact-time SLA breach
// checks, recurring reconciliation) survives restarts and is cluster-safe —
// with multiple API nodes only one fires each trigger. Tests can opt out via
// Quartz:UseInMemoryStore.
builder.Services.AddQuartz(q =>
{
    q.SchedulerName = "omniflow-scheduler";
    q.SchedulerId = "AUTO"; // unique instance ids so clustering can tell nodes apart

    if (!builder.Configuration.GetValue<bool>("Quartz:UseInMemoryStore"))
    {
        q.UsePersistentStore(s =>
        {
            s.UseProperties = true;
            s.UsePostgres(connectionString!);
            s.UseNewtonsoftJsonSerializer();
            s.UseClustering();
        });
    }

    q.AddJob<OmniFlow.Infrastructure.Scheduling.SlaReconciliationJob>(
        OmniFlow.Infrastructure.Scheduling.SlaReconciliationJob.Key,
        j => j.WithDescription("Safety-net sweep for overdue, unnotified SLA tickets"));
    q.AddTrigger(t => t
        .ForJob(OmniFlow.Infrastructure.Scheduling.SlaReconciliationJob.Key)
        .WithIdentity("sla-reconciliation-trigger", "omniflow")
        .StartAt(DateTimeOffset.UtcNow.AddMinutes(1))
        .WithSimpleSchedule(s => s.WithIntervalInMinutes(5).RepeatForever()));

    // Automated reports: sweep every minute for due schedules and email them.
    q.AddJob<OmniFlow.Infrastructure.Scheduling.ScheduledReportDispatchJob>(
        OmniFlow.Infrastructure.Scheduling.ScheduledReportDispatchJob.Key,
        j => j.WithDescription("Dispatch due scheduled reports to their recipients"));
    q.AddTrigger(t => t
        .ForJob(OmniFlow.Infrastructure.Scheduling.ScheduledReportDispatchJob.Key)
        .WithIdentity("scheduled-report-trigger", "omniflow")
        .StartAt(DateTimeOffset.UtcNow.AddMinutes(1))
        .WithSimpleSchedule(s => s.WithIntervalInMinutes(1).RepeatForever()));
});
builder.Services.AddQuartzHostedService(o => o.WaitForJobsToComplete = false);

// ═══════════════════════════════════════════════════════
// 7. MassTransit (Event-Driven Design)
// ═══════════════════════════════════════════════════════
builder.Services.AddMassTransit(x =>
{
    // IMessageScheduler.SchedulePublish() routes through the Quartz consumers
    // below, so schedules land in the persistent Quartz store instead of the
    // transport's in-memory delay buffer.
    x.AddPublishMessageScheduler();
    x.AddQuartzConsumers();

    // Transactional outbox: IPublishEndpoint.Publish() writes messages to the
    // OutboxMessage table inside the SAME transaction as SaveChanges. A
    // delivery service then moves them to the bus — a crash between "save" and
    // "publish" can no longer lose events.
    x.AddEntityFrameworkOutbox<OmniFlowDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    // Register Consumers
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.LeaveRequestNotificationConsumer>();
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.TicketSlaBreachConsumer>();
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.AssetAssignedConsumer>();
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.ApprovalCompletedConsumer>();
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.ApprovalStepActivatedConsumer>();

    // Transport is config-selected: RabbitMQ for durable, competing-consumer
    // delivery in real deployments; in-memory for broker-less local dev and
    // integration tests (Messaging:Transport).
    var transport = builder.Configuration["Messaging:Transport"] ?? "InMemory";
    if (transport.Equals("RabbitMq", StringComparison.OrdinalIgnoreCase))
    {
        x.UsingRabbitMq((context, cfg) =>
        {
            var rmq = builder.Configuration.GetSection("RabbitMq");
            cfg.Host(rmq["Host"] ?? "localhost", rmq["VirtualHost"] ?? "/", h =>
            {
                h.Username(rmq["Username"] ?? "guest");
                h.Password(rmq["Password"] ?? "guest");
            });
            cfg.UsePublishMessageScheduler();
            cfg.ConfigureEndpoints(context);
        });
    }
    else
    {
        x.UsingInMemory((context, cfg) =>
        {
            cfg.UsePublishMessageScheduler();
            cfg.ConfigureEndpoints(context);
        });
    }
});

// ═══════════════════════════════════════════════════════
// 8. ASP.NET Core Services
// ═══════════════════════════════════════════════════════
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// API versioning — non-breaking: existing /api/[controller] routes keep working
// (v1.0 is assumed when a version isn't specified) and clients can pin a version
// via ?api-version= or the Api-Version header. This puts the default-version
// machinery in place so a future breaking change ships as a declared v2 without
// disturbing v1 callers; controllers stay unannotated until then (the
// api-supported-versions header appears once a controller declares its versions).
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = Asp.Versioning.ApiVersionReader.Combine(
        new Asp.Versioning.QueryStringApiVersionReader("api-version"),
        new Asp.Versioning.HeaderApiVersionReader("Api-Version"));
}).AddMvc();

builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserContext, OmniFlow.Api.Services.UserContextService>();

// Allowed origins are config-driven so a real deployment can name its own UI
// host(s); defaults to the Vite dev origin. Under the docker/nginx setup the SPA
// is same-origin with the API so CORS never fires, but direct API access and the
// SSO round-trip still need the real origin allow-listed.
var corsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsOrigins is null || corsOrigins.Length == 0)
    corsOrigins = new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteUI", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ═══════════════════════════════════════════════════════
// 8. Health Checks & Rate Limiting
// ═══════════════════════════════════════════════════════
builder.Services.AddHealthChecks()
    .AddCheck<OmniFlow.Api.Services.DatabaseHealthCheck>("database", tags: ["ready"]);

// Brute-force guard on the token endpoint: 10 attempts per minute per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Limits are config-tunable (RateLimiting section); integration tests
    // raise them so multi-login test classes don't trip the guard.
    var tokenPermits = builder.Configuration.GetValue("RateLimiting:TokenPermitLimit", 10);
    var recoveryPermits = builder.Configuration.GetValue("RateLimiting:RecoveryPermitLimit", 5);

    options.AddPolicy("token-endpoint", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = tokenPermits,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    // Account recovery (forgot/reset password): slow enumeration + token
    // brute-force without hindering legitimate retries.
    options.AddPolicy("account-recovery", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = recoveryPermits,
                Window = TimeSpan.FromMinutes(5),
                QueueLimit = 0
            }));
});

// ═══════════════════════════════════════════════════════
// BUILD APP
// ═══════════════════════════════════════════════════════
var app = builder.Build();

// ═══════════════════════════════════════════════════════
// Middleware Pipeline
// ═══════════════════════════════════════════════════════
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// One structured completion log per request, enriched with correlation/tenant/user.
// The enricher runs after the downstream pipeline, so authentication has already
// populated the user by the time these values are read.
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        if (httpContext.Items.TryGetValue("CorrelationId", out var correlationId))
        {
            diagnosticContext.Set("CorrelationId", correlationId);
        }

        var tenantService = httpContext.RequestServices.GetService<ITenantService>();
        if (tenantService is not null)
        {
            diagnosticContext.Set("TenantId", tenantService.TenantId);
        }

        var userContext = httpContext.RequestServices.GetService<IUserContext>();
        if (userContext is not null)
        {
            diagnosticContext.Set("UserId", userContext.UserId);
        }
    };
});

// Baseline security response headers on every API response. CSP lives on the UI
// (nginx) where the HTML is served; these are the transport/sniffing/framing guards.
app.Use(async (context, next) =>
{
    var headers = context.Response.Headers;
    headers["X-Content-Type-Options"] = "nosniff";
    headers["X-Frame-Options"] = "DENY";
    headers["Referrer-Policy"] = "no-referrer";
    // HSTS only makes sense over TLS; harmless if the proxy terminates it.
    if (context.Request.IsHttps)
    {
        headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
    }
    await next();
});

// Swagger is gated on an explicit flag, not the environment: a demo deployed with
// ASPNETCORE_ENVIRONMENT=Development must not expose the API explorer unless the
// operator opts in. Defaults to on in Development, off everywhere else.
if (builder.Configuration.GetValue("Swagger:Enabled", app.Environment.IsDevelopment()))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// HTTPS redirection is skipped for the integration test host (no TLS listener),
// where a 307 to https:// would break POST/PUT/DELETE round-trips.
if (!builder.Configuration.GetValue("DisableHttpsRedirection", false))
{
    app.UseHttpsRedirection();
}
app.UseCors("AllowViteUI");
app.UseRateLimiter();

// SignalR sends the access token as a query-string parameter on the WebSocket
// connection (browsers can't set an Authorization header on the upgrade). Promote
// it to the header for /hubs paths so OpenIddict validation — which only reads the
// header — can authenticate hub connections. Must run before UseAuthentication.
app.Use(async (context, next) =>
{
    if (string.IsNullOrEmpty(context.Request.Headers.Authorization) &&
        context.Request.Path.StartsWithSegments("/hubs") &&
        context.Request.Query.TryGetValue("access_token", out var token) &&
        !string.IsNullOrEmpty(token))
    {
        context.Request.Headers.Authorization = $"Bearer {token}";
    }
    await next();
});

// Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseAuthorization();

// Audit logging must run AFTER authentication so entries carry the real user.
app.UseMiddleware<OmniFlow.Api.Middleware.AuditLoggingMiddleware>();

app.MapControllers();

// Both hubs require auth: the SPA sends the token via accessTokenFactory and the
// middleware above promotes it to the header so validation can read it.
app.MapHub<OmniFlow.Api.Hubs.DashboardHub>("/hubs/dashboard");
app.MapHub<OmniFlow.Api.Hubs.NotificationHub>("/hubs/notifications");

// Liveness: the process is up. Readiness: dependencies (database) are reachable.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
   .AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
   .AllowAnonymous();

// Prometheus scrape endpoint (/metrics), anonymous so a collector on the same
// network can pull without a token.
app.MapPrometheusScrapingEndpoint().AllowAnonymous();

// ═══════════════════════════════════════════════════════
// Database Initialization & Seeding
// ═══════════════════════════════════════════════════════
// Applies pending migrations (never drops data) and seeds idempotently.
// Failures are fatal on purpose: a half-initialized database must not serve traffic.
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<OmniFlowDbContext>();
    await context.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(scope.ServiceProvider);
}

app.Run();

// Exposes the top-level Program class to the integration test project so
// WebApplicationFactory<Program> can boot the real host.
public partial class Program;
