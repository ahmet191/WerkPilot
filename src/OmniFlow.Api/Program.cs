using MassTransit;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Application.Interfaces;
using OmniFlow.Infrastructure.Identity;
using OmniFlow.Infrastructure.Messaging;
using OmniFlow.Infrastructure.Persistence;
using OmniFlow.Infrastructure.Persistence.Interceptors;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using OmniFlow.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);

// 1. Serilog Setup
builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
    .ReadFrom.Configuration(context.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// 2. OpenTelemetry Setup
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("OmniFlow.Api"))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation());

// 3. Application Layer Setup (MediatR)
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(OmniFlow.Application.Tickets.Commands.CreateTicketCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(OmniFlow.Infrastructure.Messaging.TelegramGateway).Assembly);
});

// Telegram Bot Client Setup
var telegramToken = builder.Configuration["Telegram:BotToken"];
if (!string.IsNullOrEmpty(telegramToken))
{
    builder.Services.AddSingleton<Telegram.Bot.ITelegramBotClient>(new Telegram.Bot.TelegramBotClient(telegramToken));
    builder.Services.AddHostedService<OmniFlow.Api.Services.TelegramWebhookRegistrationService>();
}

// 4. Infrastructure Layer Setup (DbContext)
builder.Services.AddScoped<AuditableEntityInterceptor>();
builder.Services.AddDbContext<IOmniFlowDbContext, OmniFlowDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection") 
                      ?? "Host=localhost;Database=OmniFlowDb;Username=postgres;Password=postgres"));

// 5. Infrastructure Layer Setup (Services)
builder.Services.AddScoped<IMessageGateway, TelegramGateway>();
builder.Services.AddScoped<ITelegramIdentityService, TelegramIdentityService>();

// 6. MassTransit (Event-Driven Design)
builder.Services.AddMassTransit(x =>
{
    x.AddDelayedMessageScheduler();

    // Register Consumers
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.LeaveRequestNotificationConsumer>();
    x.AddConsumer<OmniFlow.Infrastructure.Messaging.Consumers.TicketSlaBreachConsumer>();

    x.UsingInMemory((context, cfg) =>
    {
        cfg.UseDelayedMessageScheduler();
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<OmniFlow.Application.Interfaces.IUserContext, OmniFlow.Api.Services.UserContextService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowViteUI", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // useful for SignalR/Cookies later
    });
});

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowViteUI");

app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<IOmniFlowDbContext>() as DbContext;
    if (context != null)
    {
        try
        {
            // Drop and recreate DB schema to apply new tables (Users, Comments, AuditLogs)
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Users representing different roles
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
                    TelegramChatId = "8920226584" // Mock default from bot testing
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

            // 2. Seed IT Support Request Template with advanced custom field rules and custom workflow definition
            if (!await context.Set<OmniFlow.Domain.Entities.TicketTemplate>().AnyAsync())
            {
                var template = new OmniFlow.Domain.Entities.TicketTemplate
                {
                    Id = Guid.Parse("2e3e5c9b-6dbb-4171-aa7a-8cf0c74fb1d2"),
                    LocalizedName = new OmniFlow.Domain.Common.MultiLingualString { Translations = new Dictionary<string, string> { { "en", "IT Support Request" }, { "tr", "BT Destek Talebi" } } },
                    DepartmentId = Guid.Parse("55555555-5555-5555-5555-555555555555"),
                    Fields = new List<OmniFlow.Domain.Entities.FieldDefinition>
                    {
                        new() { Key = "Title", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Title"), Type = "Text", IsRequired = true, ValidationRegex = "^.{5,150}$", LocalizedDescription = new OmniFlow.Domain.Common.MultiLingualString("en", "Short description of the issue") },
                        new() { Key = "Priority", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Priority"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High", "Critical" }, LocalizedDescription = new OmniFlow.Domain.Common.MultiLingualString("en", "Urgency levels") },
                        new() { Key = "Description", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Description"), Type = "Text", IsRequired = false, LocalizedDescription = new OmniFlow.Domain.Common.MultiLingualString("en", "Detailed logs or explanation") },
                        new() { Key = "AssetTag", LocalizedName = new OmniFlow.Domain.Common.MultiLingualString("en", "Asset Tag"), Type = "Text", IsRequired = false, ValidationRegex = "^[A-Z]{3}-\\d{4}$", LocalizedDescription = new OmniFlow.Domain.Common.MultiLingualString("en", "Device asset tag in ABC-1234 format") }
                    },
                    Workflow = new OmniFlow.Domain.Entities.WorkflowDefinition
                    {
                        Transitions = new List<OmniFlow.Domain.Entities.WorkflowTransition>
                        {
                            // New -> InProgress
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.New, ToState = OmniFlow.Domain.Enums.TicketState.InProgress, Trigger = "StartWork", RequiredRole = OmniFlow.Domain.Enums.UserRole.Agent },
                            
                            // InProgress -> WaitingForApproval
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.InProgress, ToState = OmniFlow.Domain.Enums.TicketState.WaitingForApproval, Trigger = "RequestApproval", RequiredRole = OmniFlow.Domain.Enums.UserRole.Agent },
                            
                            // WaitingForApproval -> Resolved (Approved)
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.WaitingForApproval, ToState = OmniFlow.Domain.Enums.TicketState.Resolved, Trigger = "Approve", RequiredRole = OmniFlow.Domain.Enums.UserRole.Approver },
                            
                            // WaitingForApproval -> InProgress (Rejected)
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.WaitingForApproval, ToState = OmniFlow.Domain.Enums.TicketState.InProgress, Trigger = "Reject", RequiredRole = OmniFlow.Domain.Enums.UserRole.Approver },
                            
                            // Resolved -> Closed
                            new() { FromState = OmniFlow.Domain.Enums.TicketState.Resolved, ToState = OmniFlow.Domain.Enums.TicketState.Closed, Trigger = "Close", RequiredRole = OmniFlow.Domain.Enums.UserRole.Requester }
                        }
                    }
                };

                context.Set<OmniFlow.Domain.Entities.TicketTemplate>().Add(template);
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
