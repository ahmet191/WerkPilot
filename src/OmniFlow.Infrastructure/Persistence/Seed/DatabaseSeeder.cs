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

        // Baseline workspace — the same code path a runtime-provisioned tenant
        // takes, pinned to the demo tenant's well-known ids.
        await TenantBaseline.SeedAsync(
            context, SeedConstants.DefaultTenantId, TenantBaselineIds.Demo, "HQ", cancellationToken);

        // Demo-only extras layered on top of the baseline.
        await SeedLegacyUsersAsync(context, cancellationToken);
        await SeedApprovalArtifactsAsync(context, cancellationToken);
        await SeedSampleTicketsAsync(context, cancellationToken);

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

    /// <summary>
    /// Seeds a spread of tickets so the dashboard and grid are populated on first
    /// boot instead of showing zeros. States are reached through the aggregate's
    /// own Transition/Assign behaviours (isSystem bypasses the role and approval
    /// guards — this is trusted seed data), so audit trails come out consistent.
    /// Gated on an empty ticket table, so it never duplicates on re-seed.
    /// </summary>
    private static async Task SeedSampleTicketsAsync(OmniFlowDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Tickets.AnyAsync(cancellationToken)) return;

        var template = await context.TicketTemplates
            .FirstOrDefaultAsync(t => t.Id == SeedConstants.ItSupportTemplateId, cancellationToken);
        if (template is null) return; // baseline template missing — nothing to hang tickets off

        static Dictionary<string, object> Data(string title, string description,
            string type, string priority, string impact, string urgency) => new()
        {
            ["Title"] = title,
            ["Description"] = description,
            ["Type"] = type,
            ["Priority"] = priority,
            ["Impact"] = impact,
            ["Urgency"] = urgency,
        };

        var requester = SeedConstants.RequesterUserId;

        // A near-breach SLA (due soon) makes the SLA column visibly urgent without
        // scheduling a breach job — seeding bypasses the command handler that would.
        var newCritical = Ticket.Create(template, requester,
            Data("VPN gateway unreachable from the branch office",
                 "Users at the branch office have been unable to connect to the VPN since 09:00.",
                 "Incident", "Critical", "High", "High"),
            slaResolutionTime: TimeSpan.FromMinutes(45));

        var newMedium = Ticket.Create(template, requester,
            Data("Laptop request for a new onboarding hire",
                 "Standard developer laptop for a backend engineer starting next week.",
                 "ServiceRequest", "Medium", "Medium", "Medium"),
            slaResolutionTime: TimeSpan.FromHours(20));

        var inProgress = Ticket.Create(template, requester,
            Data("Intermittent email delivery delays",
                 "Outbound mail to external domains is delayed by 10-15 minutes intermittently.",
                 "Incident", "High", "High", "Medium"),
            slaResolutionTime: TimeSpan.FromHours(6));

        var resolved = Ticket.Create(template, requester,
            Data("Password reset for the finance shared mailbox",
                 "Requested a password reset for the finance@ shared mailbox.",
                 "ServiceRequest", "Low", "Low", "Low"));

        var closed = Ticket.Create(template, requester,
            Data("Replace flickering monitor at desk 14",
                 "The secondary monitor at desk 14 flickers; requesting a replacement.",
                 "ServiceRequest", "Low", "Low", "Low"));

        context.Tickets.AddRange(newCritical, newMedium, inProgress, resolved, closed);
        await context.SaveChangesAsync(cancellationToken);

        var agent = await context.Users.FirstAsync(u => u.Id == SeedConstants.AgentUserId, cancellationToken);
        var approver = await context.Users.FirstAsync(u => u.Id == SeedConstants.ApproverUserId, cancellationToken);

        // Transition reads Template.Workflow, so reload each ticket with its
        // template navigation before driving it.
        async Task<Ticket> WithTemplateAsync(Guid id) =>
            await context.Tickets.Include(t => t.Template).FirstAsync(t => t.Id == id, cancellationToken);

        var ip = await WithTemplateAsync(inProgress.Id);
        ip.Assign(SeedConstants.AgentUserId, SeedConstants.AgentUserId);
        ip.Transition("StartWork", agent, isSystem: true);

        // Drive fully through the approval path so it ends Resolved (no orphaned
        // WaitingForApproval state left dangling without an approval request).
        var rs = await WithTemplateAsync(resolved.Id);
        rs.Assign(SeedConstants.AgentUserId, SeedConstants.AgentUserId);
        rs.Transition("StartWork", agent, isSystem: true);
        rs.Transition("RequestApproval", agent, isSystem: true);
        rs.Transition("Approve", approver, isSystem: true);

        var cl = await WithTemplateAsync(closed.Id);
        cl.Assign(SeedConstants.AgentUserId, SeedConstants.AgentUserId);
        cl.Transition("StartWork", agent, isSystem: true);
        cl.Transition("RequestApproval", agent, isSystem: true);
        cl.Transition("Approve", approver, isSystem: true);
        cl.Transition("Close", approver, isSystem: true);

        await context.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedLegacyUsersAsync(OmniFlowDbContext context, CancellationToken cancellationToken)
    {
        if (await context.Users.AnyAsync(cancellationToken)) return;

        // The department itself comes from TenantBaseline.
        var department = new { Id = SeedConstants.HqDepartmentId };

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

}
