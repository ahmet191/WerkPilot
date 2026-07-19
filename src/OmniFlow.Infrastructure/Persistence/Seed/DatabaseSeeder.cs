using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Persistence.Seed;

/// <summary>
/// Seeds the default tenant and demo domain data. Idempotent: every block is
/// gated on the data it creates, so re-running on an existing database is a no-op.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var context = serviceProvider.GetRequiredService<OmniFlowDbContext>();
        var tenantService = serviceProvider.GetRequiredService<ITenantService>();
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseSeeder");

        // Seeding runs outside an HTTP request, so there is no tenant claim to resolve.
        // Pin the ambient tenant so the auditing interceptor stamps seeded rows correctly
        // and the global query filters evaluate against the default tenant.
        tenantService.SetTenantId(SeedConstants.DefaultTenantId);

        if (!await context.Tenants.AnyAsync(cancellationToken))
        {
            context.Tenants.Add(new Tenant
            {
                Id = SeedConstants.DefaultTenantId,
                Name = "WerkPilot Demo",
                Domain = "demo.werkpilot.local",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            });
            await context.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded default tenant {TenantId}", SeedConstants.DefaultTenantId);
        }

        // Identity (roles, admin user, OpenIddict scopes/client) is idempotent internally.
        await IdentitySeedData.SeedAsync(serviceProvider);

        await SeedLegacyUsersAsync(context, cancellationToken);
        await SeedServiceDeskAsync(context, cancellationToken);
        await SeedTicketTemplateAsync(context, cancellationToken);
        await SeedSlaConfigurationsAsync(context, cancellationToken);
        await SeedMenusAsync(context, cancellationToken);
        await SeedApprovalArtifactsAsync(context, cancellationToken);

        logger.LogInformation("Database seeding completed");
    }

    /// <summary>
    /// Approval-engine seed data. Unlike the bulk seeds above this is fully
    /// idempotent per artifact (not gated on an empty database), so existing
    /// dev databases pick up the approvals menu, tenant leave policy and
    /// template approval policy on the next boot.
    /// </summary>
    private static async Task SeedApprovalArtifactsAsync(OmniFlowDbContext context, CancellationToken cancellationToken)
    {
        var dirty = false;

        // 1. "My Approvals" menu entry (root level, right after Dashboard).
        if (!await context.AppMenus.AnyAsync(m => m.TitleKey == "sidebar.approvals", cancellationToken))
        {
            context.AppMenus.Add(new AppMenu
            {
                Id = Guid.NewGuid(),
                TitleKey = "sidebar.approvals",
                Path = "/approvals",
                Icon = "AuditOutlined",
                OrderIndex = 15,
                IsActive = true
            });
            dirty = true;
        }

        // 2. Tenant-wide default leave approval chain: manager → HR admin.
        var tenant = await context.Tenants
            .FirstOrDefaultAsync(t => t.Id == SeedConstants.DefaultTenantId, cancellationToken);
        if (tenant is not null && tenant.LeaveApprovalPolicy is null)
        {
            tenant.LeaveApprovalPolicy = new ApprovalDefinition
            {
                Steps =
                {
                    new ApprovalStepDefinition { Order = 1, ApproverType = ApproverType.ManagerOfRequester },
                    new ApprovalStepDefinition { Order = 2, ApproverType = ApproverType.Role, ApproverRole = "HRAdmin" }
                }
            };
            dirty = true;
        }

        // 3. Approval policy on the IT template's RequestApproval transition:
        //    Charlie (the seeded approver) decides.
        var template = await context.TicketTemplates
            .FirstOrDefaultAsync(t => t.Id == SeedConstants.ItSupportTemplateId, cancellationToken);
        var requestApproval = template?.Workflow.Transitions
            .FirstOrDefault(tr => tr.Trigger == "RequestApproval");
        if (template is not null && requestApproval is not null && requestApproval.ApprovalPolicy is null)
        {
            requestApproval.ApprovalPolicy = new ApprovalDefinition
            {
                Steps =
                {
                    new ApprovalStepDefinition
                    {
                        Order = 1,
                        ApproverType = ApproverType.SpecificUser,
                        ApproverUserId = SeedConstants.ApproverUserId
                    }
                }
            };
            // JSONB value conversion compares by reference — mark explicitly.
            context.Entry(template).Property(t => t.Workflow).IsModified = true;
            dirty = true;
        }

        if (dirty)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private static async Task SeedLegacyUsersAsync(OmniFlowDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken)) return;

        var department = new Department
        {
            Id = SeedConstants.HqDepartmentId,
            LocalizedName = new MultiLingualString { Translations = new Dictionary<string, string> { { "en", "HQ" }, { "tr", "Merkez" } } }
        };
        context.Departments.Add(department);

        context.Users.AddRange(
            new User
            {
                Id = SeedConstants.RequesterUserId,
                Name = "Alice Requester",
                Email = "alice@omniflow.com",
                Role = UserRole.Requester,
                DepartmentId = department.Id
            },
            new User
            {
                Id = SeedConstants.AgentUserId,
                Name = "Bob Agent",
                Email = "bob@omniflow.com",
                Role = UserRole.Agent,
                DepartmentId = department.Id
            },
            new User
            {
                Id = SeedConstants.ApproverUserId,
                Name = "Charlie Approver",
                Email = "charlie@omniflow.com",
                Role = UserRole.Approver,
                DepartmentId = department.Id
            },
            new User
            {
                Id = SeedConstants.AdminUserId,
                Name = "Admin User",
                Email = "admin@werkpilot.com",
                Role = UserRole.Administrator,
                DepartmentId = department.Id
            });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSlaConfigurationsAsync(OmniFlowDbContext context, CancellationToken cancellationToken)
    {
        if (await context.SLAConfigurations.AnyAsync(cancellationToken)) return;

        // Default resolution/first-response targets per priority. Tenants can
        // tune these via the SLA configuration admin screen.
        (string Priority, int ResolutionMinutes, int FirstResponseMinutes)[] defaults =
        [
            ("Critical", 4 * 60, 30),
            ("High", 8 * 60, 60),
            ("Medium", 24 * 60, 4 * 60),
            ("Low", 48 * 60, 8 * 60),
        ];

        foreach (var (priority, resolution, firstResponse) in defaults)
        {
            context.SLAConfigurations.Add(new SLAConfiguration
            {
                Name = $"{priority} priority SLA",
                DepartmentId = SeedConstants.HqDepartmentId,
                PriorityLevel = priority,
                ResolutionTimeMinutes = resolution,
                FirstResponseTimeMinutes = firstResponse
            });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedServiceDeskAsync(OmniFlowDbContext context, CancellationToken cancellationToken)
    {
        if (await context.ServiceDesks.AnyAsync(cancellationToken)) return;

        context.ServiceDesks.Add(new ServiceDesk
        {
            Id = SeedConstants.ItServiceDeskId,
            Name = "IT Service Desk",
            Description = "Global IT Support",
            DepartmentId = SeedConstants.HqDepartmentId,
            IsPublicToAllEmployees = true
        });

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedTicketTemplateAsync(OmniFlowDbContext context, CancellationToken cancellationToken)
    {
        if (await context.TicketTemplates.AnyAsync(cancellationToken)) return;

        var template = new TicketTemplate
        {
            Id = SeedConstants.ItSupportTemplateId,
            LocalizedName = new MultiLingualString { Translations = new Dictionary<string, string> { { "en", "IT Support Request" }, { "tr", "BT Destek Talebi" } } },
            ServiceDeskId = SeedConstants.ItServiceDeskId,
            Fields = new List<FieldDefinition>
            {
                new() { Key = "Title", LocalizedName = new MultiLingualString("en", "Title"), Type = "Text", IsRequired = true, ValidationRegex = "^.{5,150}$", LocalizedDescription = new MultiLingualString("en", "Short description of the issue") },
                new() { Key = "Type", LocalizedName = new MultiLingualString("en", "Type"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Incident", "ServiceRequest", "Problem", "Change" } },
                new() { Key = "Priority", LocalizedName = new MultiLingualString("en", "Priority"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High", "Critical" }, LocalizedDescription = new MultiLingualString("en", "Urgency levels") },
                new() { Key = "Impact", LocalizedName = new MultiLingualString("en", "Impact"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High" } },
                new() { Key = "Urgency", LocalizedName = new MultiLingualString("en", "Urgency"), Type = "Dropdown", IsRequired = true, DropdownOptions = new List<string> { "Low", "Medium", "High" } },
                new() { Key = "Description", LocalizedName = new MultiLingualString("en", "Description"), Type = "Text", IsRequired = false, LocalizedDescription = new MultiLingualString("en", "Detailed logs or explanation") },
                new() { Key = "AssetTag", LocalizedName = new MultiLingualString("en", "Asset Tag"), Type = "Text", IsRequired = false, ValidationRegex = @"^[A-Z]{3}-\d{4}$", LocalizedDescription = new MultiLingualString("en", "Device asset tag in ABC-1234 format") }
            },
            Workflow = new WorkflowDefinition
            {
                Transitions = new List<WorkflowTransition>
                {
                    new() { FromState = TicketState.New, ToState = TicketState.InProgress, Trigger = "StartWork", RequiredRole = UserRole.Agent },
                    new() { FromState = TicketState.InProgress, ToState = TicketState.WaitingForApproval, Trigger = "RequestApproval", RequiredRole = UserRole.Agent },
                    new() { FromState = TicketState.WaitingForApproval, ToState = TicketState.Resolved, Trigger = "Approve", RequiredRole = UserRole.Approver },
                    new() { FromState = TicketState.WaitingForApproval, ToState = TicketState.InProgress, Trigger = "Reject", RequiredRole = UserRole.Approver },
                    new() { FromState = TicketState.Resolved, ToState = TicketState.Closed, Trigger = "Close", RequiredRole = UserRole.Requester }
                }
            }
        };

        context.TicketTemplates.Add(template);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedMenusAsync(OmniFlowDbContext context, CancellationToken cancellationToken)
    {
        if (await context.AppMenus.AnyAsync(cancellationToken)) return;

        var dashboard = new AppMenu { Id = Guid.NewGuid(), TitleKey = "sidebar.dashboard", Path = "/", Icon = "DashboardOutlined", OrderIndex = 10 };
        var itsmParent = new AppMenu { Id = Guid.NewGuid(), TitleKey = "sidebar.itsm", Icon = "AppstoreOutlined", OrderIndex = 20 };
        var hrParent = new AppMenu { Id = Guid.NewGuid(), TitleKey = "sidebar.hr", Icon = "TeamOutlined", OrderIndex = 30 };
        var adminParent = new AppMenu { Id = Guid.NewGuid(), TitleKey = "sidebar.admin", Icon = "SettingOutlined", OrderIndex = 40 };

        var menus = new List<AppMenu>
        {
            dashboard,
            itsmParent,
            new() { Id = Guid.NewGuid(), ParentId = itsmParent.Id, TitleKey = "sidebar.servicedeskportal", Path = "/servicedeskportal", Icon = "AppstoreOutlined", OrderIndex = 10 },
            new() { Id = Guid.NewGuid(), ParentId = itsmParent.Id, TitleKey = "sidebar.tickets", Path = "/tickets", Icon = "ContainerOutlined", OrderIndex = 20 },
            new() { Id = Guid.NewGuid(), ParentId = itsmParent.Id, TitleKey = "sidebar.assets", Path = "/assets", Icon = "LaptopOutlined", OrderIndex = 30 },

            hrParent,
            new() { Id = Guid.NewGuid(), ParentId = hrParent.Id, TitleKey = "sidebar.directory", Path = "/directory", Icon = "TeamOutlined", OrderIndex = 10 },
            new() { Id = Guid.NewGuid(), ParentId = hrParent.Id, TitleKey = "sidebar.orgchart", Path = "/orgchart", Icon = "DashboardOutlined", OrderIndex = 20 },
            new() { Id = Guid.NewGuid(), ParentId = hrParent.Id, TitleKey = "sidebar.leaves", Path = "/leaves", Icon = "CalendarOutlined", OrderIndex = 30 },
            new() { Id = Guid.NewGuid(), ParentId = hrParent.Id, TitleKey = "sidebar.expenses", Path = "/expenses", Icon = "DollarOutlined", OrderIndex = 40 },

            adminParent,
            new() { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.departments", Path = "/departments", Icon = "TeamOutlined", OrderIndex = 10 },
            new() { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.servicedesks", Path = "/servicedesks", Icon = "ContainerOutlined", OrderIndex = 20 },
            new() { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.auditlogs", Path = "/auditlogs", Icon = "SafetyCertificateOutlined", OrderIndex = 30 },
            new() { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.users", Path = "/users", Icon = "UserOutlined", OrderIndex = 40 },
            new() { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.templates", Path = "/templates", Icon = "FileTextOutlined", OrderIndex = 50 },
            new() { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.categories", Path = "/categories", Icon = "AppstoreAddOutlined", OrderIndex = 60 },
            new() { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.slas", Path = "/slas", Icon = "FieldTimeOutlined", OrderIndex = 70 },
            new() { Id = Guid.NewGuid(), ParentId = adminParent.Id, TitleKey = "sidebar.menus", Path = "/menus", Icon = "MenuOutlined", OrderIndex = 80 }
        };

        context.AppMenus.AddRange(menus);
        await context.SaveChangesAsync(cancellationToken);
    }
}
