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
            .AddHttpClientInstrumentation();

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
        var aspNetCoreBuilder = options.UseAspNetCore()
               .EnableTokenEndpointPassthrough()
               .EnableUserInfoEndpointPassthrough();

        if (builder.Environment.IsDevelopment())
        {
            // Allow HTTP locally only.
            aspNetCoreBuilder.DisableTransportSecurityRequirement();

            // Development certificates are generated once and persisted in the
            // user certificate store, so issued tokens survive API restarts.
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();
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
// 8. Health Checks & Rate Limiting
// ═══════════════════════════════════════════════════════
builder.Services.AddHealthChecks()
    .AddCheck<OmniFlow.Api.Services.DatabaseHealthCheck>("database", tags: ["ready"]);

// Brute-force guard on the token endpoint: 10 attempts per minute per client IP.
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("token-endpoint", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
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

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowViteUI");
app.UseRateLimiter();

// Authentication & Authorization (order matters!)
app.UseAuthentication();
app.UseAuthorization();

// Audit logging must run AFTER authentication so entries carry the real user.
app.UseMiddleware<OmniFlow.Api.Middleware.AuditLoggingMiddleware>();

app.MapControllers();

// TODO(Phase 5): require auth on the hub once the SPA sends access tokens on the
// SignalR connection (accessTokenFactory) and query-string token extraction is wired.
app.MapHub<OmniFlow.Api.Hubs.DashboardHub>("/hubs/dashboard").AllowAnonymous();

// Liveness: the process is up. Readiness: dependencies (database) are reachable.
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false })
   .AllowAnonymous();
app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains("ready") })
   .AllowAnonymous();

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
