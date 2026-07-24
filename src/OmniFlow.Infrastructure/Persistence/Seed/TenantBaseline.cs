using Microsoft.EntityFrameworkCore;
using OmniFlow.Domain.Common;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Infrastructure.Persistence.Seed;

/// <summary>
/// Identifiers for a tenant's baseline records. The demo tenant pins them to
/// <see cref="SeedConstants"/> (tests and fixtures reference those GUIDs);
/// tenants created at runtime get fresh ones.
/// </summary>
public record TenantBaselineIds(Guid DepartmentId, Guid ServiceDeskId, Guid TemplateId)
{
    public static TenantBaselineIds New() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

    public static TenantBaselineIds Demo => new(
        SeedConstants.HqDepartmentId,
        SeedConstants.ItServiceDeskId,
        SeedConstants.ItSupportTemplateId);
}

/// <summary>
/// Everything a tenant needs to be usable the moment it is created: an
/// organisational root, a service desk with a working ticket template, SLA
/// targets, navigation, the permission matrix and a leave approval chain.
///
/// Shared by the demo seeder and runtime tenant provisioning so a new customer
/// gets exactly the workspace we test against. Each block is idempotent and —
/// because these are tenant-scoped entities behind the global query filter —
/// the "already seeded?" checks answer per tenant.
///
/// The caller must pin the ambient tenant (ITenantService.SetTenantId) first.
/// </summary>
public static class TenantBaseline
{
    public static async Task SeedAsync(
        OmniFlowDbContext context,
        Guid tenantId,
        TenantBaselineIds ids,
        string departmentName = "Head Office",
        CancellationToken cancellationToken = default)
    {
        await SeedDepartmentAsync(context, tenantId, ids, departmentName, cancellationToken);
        await SeedServiceDeskAsync(context, tenantId, ids, cancellationToken);
        await SeedTicketTemplateAsync(context, tenantId, ids, cancellationToken);
        await SeedSlaConfigurationsAsync(context, tenantId, ids, cancellationToken);
        await SeedMenusAsync(context, tenantId, cancellationToken);
        await SeedPermissionMatrixAsync(context, tenantId, cancellationToken);
        await SeedLeaveApprovalPolicyAsync(context, tenantId, cancellationToken);
    }

    private static async Task SeedDepartmentAsync(
        OmniFlowDbContext context, Guid tenantId, TenantBaselineIds ids, string departmentName, CancellationToken cancellationToken)
    {
        if (await context.Departments.AnyAsync(cancellationToken)) return;

        context.Departments.Add(new Department
        {
            Id = ids.DepartmentId,
            TenantId = tenantId,
            LocalizedName = new MultiLingualString
            {
                Translations = new Dictionary<string, string> { { "en", departmentName }, { "tr", departmentName } }
            }
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedServiceDeskAsync(
        OmniFlowDbContext context, Guid tenantId, TenantBaselineIds ids, CancellationToken cancellationToken)
    {
        if (await context.ServiceDesks.AnyAsync(cancellationToken)) return;

        context.ServiceDesks.Add(new ServiceDesk
        {
            Id = ids.ServiceDeskId,
            TenantId = tenantId,
            Name = "IT Service Desk",
            Description = "Global IT Support",
            DepartmentId = ids.DepartmentId,
            IsPublicToAllEmployees = true
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedTicketTemplateAsync(
        OmniFlowDbContext context, Guid tenantId, TenantBaselineIds ids, CancellationToken cancellationToken)
    {
        if (await context.TicketTemplates.AnyAsync(cancellationToken)) return;

        context.TicketTemplates.Add(new TicketTemplate
        {
            Id = ids.TemplateId,
            TenantId = tenantId,
            LocalizedName = new MultiLingualString
            {
                Translations = new Dictionary<string, string> { { "en", "IT Support Request" }, { "tr", "BT Destek Talebi" } }
            },
            ServiceDeskId = ids.ServiceDeskId,
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
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedSlaConfigurationsAsync(
        OmniFlowDbContext context, Guid tenantId, TenantBaselineIds ids, CancellationToken cancellationToken)
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
                TenantId = tenantId,
                Name = $"{priority} priority SLA",
                DepartmentId = ids.DepartmentId,
                PriorityLevel = priority,
                ResolutionTimeMinutes = resolution,
                FirstResponseTimeMinutes = firstResponse
            });
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedMenusAsync(OmniFlowDbContext context, Guid tenantId, CancellationToken cancellationToken)
    {
        var existingPaths = await context.AppMenus
            .Where(m => m.Path != null)
            .Select(m => m.Path!)
            .ToListAsync(cancellationToken);

        AppMenu? hrParent = await context.AppMenus.FirstOrDefaultAsync(m => m.TitleKey == "sidebar.hr", cancellationToken);
        AppMenu? adminParent = await context.AppMenus.FirstOrDefaultAsync(m => m.TitleKey == "sidebar.admin", cancellationToken);

        if (!await context.AppMenus.AnyAsync(cancellationToken))
        {
            var dashboard = new AppMenu { Id = Guid.NewGuid(), TenantId = tenantId, TitleKey = "sidebar.dashboard", Path = "/", Icon = "DashboardOutlined", OrderIndex = 10 };
            var approvals = new AppMenu { Id = Guid.NewGuid(), TenantId = tenantId, TitleKey = "sidebar.approvals", Path = "/approvals", Icon = "AuditOutlined", OrderIndex = 15, IsActive = true };
            var itsmParent = new AppMenu { Id = Guid.NewGuid(), TenantId = tenantId, TitleKey = "sidebar.itsm", Icon = "AppstoreOutlined", OrderIndex = 20 };
            hrParent = new AppMenu { Id = Guid.NewGuid(), TenantId = tenantId, TitleKey = "sidebar.hr", Icon = "TeamOutlined", OrderIndex = 30 };
            adminParent = new AppMenu { Id = Guid.NewGuid(), TenantId = tenantId, TitleKey = "sidebar.admin", Icon = "SettingOutlined", OrderIndex = 40 };

            AppMenu Child(AppMenu parent, string titleKey, string path, string icon, int order) => new()
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                ParentId = parent.Id,
                TitleKey = titleKey,
                Path = path,
                Icon = icon,
                OrderIndex = order
            };

            context.AppMenus.AddRange(
                dashboard,
                approvals,
                itsmParent,
                Child(itsmParent, "sidebar.servicedeskportal", "/servicedeskportal", "AppstoreOutlined", 10),
                Child(itsmParent, "sidebar.tickets", "/tickets", "ContainerOutlined", 20),
                Child(itsmParent, "sidebar.assets", "/assets", "LaptopOutlined", 30),

                hrParent,
                Child(hrParent, "sidebar.directory", "/directory", "TeamOutlined", 10),
                Child(hrParent, "sidebar.hrCenter", "/hr/center", "IdcardOutlined", 15),
                Child(hrParent, "sidebar.onboarding", "/hr/onboarding", "UserAddOutlined", 18),
                Child(hrParent, "sidebar.okrs", "/hr/okrs", "AimOutlined", 19),
                Child(hrParent, "sidebar.skills", "/hr/skills", "TrophyOutlined", 21),
                Child(hrParent, "sidebar.trainings", "/hr/trainings", "ReadOutlined", 22),
                Child(hrParent, "sidebar.orgchart", "/orgchart", "DashboardOutlined", 25),
                Child(hrParent, "sidebar.leaves", "/leaves", "CalendarOutlined", 30),
                Child(hrParent, "sidebar.expenses", "/expenses", "DollarOutlined", 40),

                adminParent,
                Child(adminParent, "sidebar.departments", "/departments", "TeamOutlined", 10),
                Child(adminParent, "sidebar.servicedesks", "/servicedesks", "ContainerOutlined", 20),
                Child(adminParent, "sidebar.auditlogs", "/auditlogs", "SafetyCertificateOutlined", 30),
                Child(adminParent, "sidebar.users", "/users", "UserOutlined", 40),
                Child(adminParent, "sidebar.templates", "/templates", "FileTextOutlined", 50),
                Child(adminParent, "sidebar.categories", "/categories", "AppstoreAddOutlined", 60),
                Child(adminParent, "sidebar.slas", "/slas", "FieldTimeOutlined", 70),
                Child(adminParent, "sidebar.visibility", "/settings/visibility", "EyeOutlined", 75),
                Child(adminParent, "sidebar.permissions", "/settings/permissions", "SafetyCertificateOutlined", 78),
                Child(adminParent, "sidebar.reports", "/reports", "BarChartOutlined", 80),
                Child(adminParent, "sidebar.menus", "/menus", "MenuOutlined", 85));

            await context.SaveChangesAsync(cancellationToken);
            return;
        }

        // Reconcile missing menu items into existing database
        var toAdd = new List<AppMenu>();
        void EnsureChild(AppMenu? parent, string titleKey, string path, string icon, int order)
        {
            if (parent != null && !existingPaths.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                toAdd.Add(new AppMenu
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    ParentId = parent.Id,
                    TitleKey = titleKey,
                    Path = path,
                    Icon = icon,
                    OrderIndex = order
                });
            }
        }

        EnsureChild(hrParent, "sidebar.hrCenter", "/hr/center", "IdcardOutlined", 15);
        EnsureChild(hrParent, "sidebar.onboarding", "/hr/onboarding", "UserAddOutlined", 18);
        EnsureChild(hrParent, "sidebar.okrs", "/hr/okrs", "AimOutlined", 19);
        EnsureChild(hrParent, "sidebar.skills", "/hr/skills", "TrophyOutlined", 21);
        EnsureChild(hrParent, "sidebar.trainings", "/hr/trainings", "ReadOutlined", 22);
        EnsureChild(adminParent, "sidebar.visibility", "/settings/visibility", "EyeOutlined", 75);
        EnsureChild(adminParent, "sidebar.deptStudio", "/settings/department-studio", "BankOutlined", 76);
        EnsureChild(adminParent, "sidebar.formDesigner", "/settings/form-designer", "FileTextOutlined", 77);
        EnsureChild(adminParent, "sidebar.permissions", "/settings/permissions", "SafetyCertificateOutlined", 78);
        EnsureChild(adminParent, "sidebar.slaMatrix", "/settings/sla-matrix", "FieldTimeOutlined", 79);
        EnsureChild(adminParent, "sidebar.reports", "/reports", "BarChartOutlined", 80);

        if (toAdd.Count > 0)
        {
            context.AppMenus.AddRange(toAdd);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    /// <summary>
    /// The tenant's default role → permission grants. Administrator is absent by
    /// design: it always holds every permission (see Permissions.SuperRole).
    /// </summary>
    private static async Task SeedPermissionMatrixAsync(OmniFlowDbContext context, Guid tenantId, CancellationToken cancellationToken)
    {
        if (await context.RolePermissions.AnyAsync(cancellationToken)) return;

        foreach (var (roleName, permissions) in Permissions.DefaultMatrix)
        {
            foreach (var permission in permissions)
            {
                context.RolePermissions.Add(new RolePermission
                {
                    TenantId = tenantId,
                    RoleName = roleName,
                    Permission = permission
                });
            }
        }
        await context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Tenant-wide default leave approval chain: manager → HR admin.</summary>
    private static async Task SeedLeaveApprovalPolicyAsync(OmniFlowDbContext context, Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken);
        if (tenant is null || tenant.LeaveApprovalPolicy is not null) return;

        tenant.LeaveApprovalPolicy = new ApprovalDefinition
        {
            Steps =
            {
                new ApprovalStepDefinition { Order = 1, ApproverType = ApproverType.ManagerOfRequester },
                new ApprovalStepDefinition { Order = 2, ApproverType = ApproverType.Role, ApproverRole = "HRAdmin" }
            }
        };
        await context.SaveChangesAsync(cancellationToken);
    }
}
