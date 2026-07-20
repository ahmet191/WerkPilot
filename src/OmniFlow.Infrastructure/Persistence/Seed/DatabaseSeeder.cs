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
