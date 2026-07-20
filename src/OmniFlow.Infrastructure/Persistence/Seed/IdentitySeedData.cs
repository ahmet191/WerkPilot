using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using OmniFlow.Domain.Entities;
using OmniFlow.Infrastructure.Identity;

namespace OmniFlow.Infrastructure.Persistence.Seed;

public static class IdentitySeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var applicationManager = serviceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        // ── 1. Seed Roles ──
        var roles = new[] { "Administrator", "HRAdmin", "ITAgent", "Requester" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            }
        }

        // ── 2. Seed Admin User ──
        var adminEmail = "admin@werkpilot.com";
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                Id = SeedConstants.AdminUserId,
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                TenantId = SeedConstants.DefaultTenantId
            };

            var result = await userManager.CreateAsync(adminUser, "Admin@123");
            if (result.Succeeded)
            {
                foreach (var roleName in roles)
                {
                    await userManager.AddToRoleAsync(adminUser, roleName);
                }
            }
        }

        // ── 2b. Seed non-admin demo logins ──
        // The demo tenant ships one login per role so RBAC is exercisable
        // (and demoable) without hand-provisioning accounts first.
        (Guid Id, string Email, string Role)[] demoAccounts =
        [
            (SeedConstants.AgentUserId, "bob@omniflow.com", "ITAgent"),
            (SeedConstants.ApproverUserId, "charlie@omniflow.com", "HRAdmin"),
            (SeedConstants.RequesterUserId, "alice@omniflow.com", "Requester"),
        ];

        foreach (var (id, email, role) in demoAccounts)
        {
            if (await userManager.FindByEmailAsync(email) != null) continue;

            var account = new ApplicationUser
            {
                Id = id,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                TenantId = SeedConstants.DefaultTenantId
            };

            var created = await userManager.CreateAsync(account, "Demo@12345");
            if (created.Succeeded)
            {
                await userManager.AddToRoleAsync(account, role);
            }
        }

        // ── 3. Seed OpenIddict Scopes ──
        var scopeManager = serviceProvider.GetRequiredService<IOpenIddictScopeManager>();
        
        var scopesToSeed = new[]
        {
            new OpenIddictScopeDescriptor { Name = OpenIddictConstants.Scopes.Email, DisplayName = "Email" },
            new OpenIddictScopeDescriptor { Name = OpenIddictConstants.Scopes.Profile, DisplayName = "Profile" },
            new OpenIddictScopeDescriptor { Name = OpenIddictConstants.Scopes.Roles, DisplayName = "Roles" },
        };

        foreach (var scopeDescriptor in scopesToSeed)
        {
            if (await scopeManager.FindByNameAsync(scopeDescriptor.Name!) == null)
            {
                await scopeManager.CreateAsync(scopeDescriptor);
            }
        }

        // ── 4. Seed OpenIddict Application (SPA Client) ──
        if (await applicationManager.FindByClientIdAsync("werkpilot-spa") == null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "werkpilot-spa",
                DisplayName = "WerkPilot SPA Frontend",
                Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.Password,
                    OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile,
                    OpenIddictConstants.Permissions.Scopes.Roles,
                }
            });
        }
    }
}
