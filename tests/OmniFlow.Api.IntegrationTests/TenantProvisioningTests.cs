using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Infrastructure.Identity;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// Tenant onboarding and — the point of a multi-tenant platform — isolation
/// between the tenants it creates. A workspace provisioned at runtime must be
/// immediately usable and must never see another tenant's data.
/// </summary>
public class TenantProvisioningTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string PlatformAdmin = "admin@werkpilot.com";
    private const string PlatformAdminPass = "Admin@123";
    private const string AgentUser = "bob@omniflow.com";
    private const string DemoPass = "Demo@12345";

    private readonly CustomWebApplicationFactory _factory;

    public TenantProvisioningTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Provisioned_tenant_is_isolated_and_immediately_usable()
    {
        var platform = await ClientAsync(PlatformAdmin, PlatformAdminPass);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminEmail = $"admin.{suffix}@acme.test";
        var create = await platform.PostAsJsonAsync("/api/platform/tenants", new
        {
            name = $"Acme {suffix}",
            domain = $"acme-{suffix}.werkpilot.local",
            adminEmail,
            adminName = "Acme Admin"
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);

        // The new admin arrives passwordless (invited by email); set a password
        // the way the emailed reset link would.
        await SetPasswordViaResetTokenAsync(adminEmail, "AcmePass@123");
        var acme = await ClientAsync(adminEmail, "AcmePass@123");

        // ── Isolation: the demo tenant's data is invisible ──
        var acmeUsers = await ItemsAsync(acme, "/api/users?pageSize=100");
        Assert.Single(acmeUsers);                                  // only their own admin
        Assert.All(acmeUsers, u => Assert.Equal(adminEmail, u.GetProperty("email").GetString()));

        var acmeTickets = await ItemsAsync(acme, "/api/tickets?pageSize=100");
        Assert.Empty(acmeTickets);                                 // demo tickets stay in the demo tenant

        // ...and the demo tenant does not see the new tenant's admin either.
        var demoUsers = await ItemsAsync(platform, "/api/users?pageSize=100");
        Assert.DoesNotContain(demoUsers, u => u.GetProperty("email").GetString() == adminEmail);

        // ── Usable: baseline workspace came with the tenant ──
        var acmeMenus = await acme.GetFromJsonAsync<JsonElement>("/api/menu");
        Assert.True(acmeMenus.GetArrayLength() > 0);

        var acmeMatrix = await acme.GetFromJsonAsync<JsonElement>("/api/permissions/matrix");
        Assert.True(acmeMatrix.GetProperty("matrix").EnumerateObject().Any());

        var acmeTemplates = await ItemsAsync(acme, "/api/templates?pageSize=10");
        Assert.NotEmpty(acmeTemplates);

        // The new admin can actually file a ticket in their own workspace.
        var templateId = acmeTemplates[0].GetProperty("id").GetGuid();
        var requesterId = acmeUsers[0].GetProperty("id").GetGuid();
        var ticket = await acme.PostAsJsonAsync("/api/tickets", new
        {
            templateId,
            requesterId,
            dynamicData = new Dictionary<string, object>
            {
                ["Title"] = "First ticket in the new workspace",
                ["Type"] = "Incident",
                ["Priority"] = "Medium",
                ["Impact"] = "Low",
                ["Urgency"] = "Low",
            }
        });
        Assert.Equal(HttpStatusCode.OK, ticket.StatusCode);
    }

    [Fact]
    public async Task Suspended_tenant_cannot_sign_in_until_reactivated()
    {
        var platform = await ClientAsync(PlatformAdmin, PlatformAdminPass);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminEmail = $"admin.{suffix}@suspend.test";
        var create = await platform.PostAsJsonAsync("/api/platform/tenants", new
        {
            name = $"Suspend {suffix}",
            domain = $"suspend-{suffix}.werkpilot.local",
            adminEmail,
            adminName = "Suspend Admin"
        });
        var tenantId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("tenantId").GetGuid();

        await SetPasswordViaResetTokenAsync(adminEmail, "Suspend@123");
        Assert.Equal(HttpStatusCode.OK, (await TokenAsync(adminEmail, "Suspend@123")).Status);

        Assert.Equal(HttpStatusCode.NoContent,
            (await platform.PostAsync($"/api/platform/tenants/{tenantId}/deactivate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await TokenAsync(adminEmail, "Suspend@123")).Status);

        Assert.Equal(HttpStatusCode.NoContent,
            (await platform.PostAsync($"/api/platform/tenants/{tenantId}/activate", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await TokenAsync(adminEmail, "Suspend@123")).Status);
    }

    [Fact]
    public async Task Duplicate_domain_is_rejected()
    {
        var platform = await ClientAsync(PlatformAdmin, PlatformAdminPass);
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var domain = $"dup-{suffix}.werkpilot.local";

        var first = await platform.PostAsJsonAsync("/api/platform/tenants", new
        {
            name = "First", domain, adminEmail = $"first.{suffix}@dup.test", adminName = "First Admin"
        });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await platform.PostAsJsonAsync("/api/platform/tenants", new
        {
            name = "Second", domain, adminEmail = $"second.{suffix}@dup.test", adminName = "Second Admin"
        });
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Tenant_administrator_is_not_a_platform_administrator()
    {
        // A tenant Administrator holds every *tenant* permission, but platform
        // endpoints sit above that boundary.
        var agent = await ClientAsync(AgentUser, DemoPass);
        Assert.Equal(HttpStatusCode.Forbidden, (await agent.GetAsync("/api/platform/tenants")).StatusCode);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var platform = await ClientAsync(PlatformAdmin, PlatformAdminPass);
        var create = await platform.PostAsJsonAsync("/api/platform/tenants", new
        {
            name = $"Boundary {suffix}",
            domain = $"boundary-{suffix}.werkpilot.local",
            adminEmail = $"admin.{suffix}@boundary.test",
            adminName = "Boundary Admin"
        });
        create.EnsureSuccessStatusCode();

        await SetPasswordViaResetTokenAsync($"admin.{suffix}@boundary.test", "Boundary@123");
        var tenantAdmin = await ClientAsync($"admin.{suffix}@boundary.test", "Boundary@123");
        Assert.Equal(HttpStatusCode.Forbidden, (await tenantAdmin.GetAsync("/api/platform/tenants")).StatusCode);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private sealed record TokenResult(HttpStatusCode Status, JsonElement Json);

    private async Task<TokenResult> TokenAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid email profile roles offline_access",
        }));
        return new TokenResult(resp.StatusCode, await resp.Content.ReadFromJsonAsync<JsonElement>());
    }

    private async Task<HttpClient> ClientAsync(string username, string password)
    {
        var token = await TokenAsync(username, password);
        Assert.True(token.Status == HttpStatusCode.OK, $"login failed for {username}: {token.Json.GetRawText()}");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token.Json.GetProperty("access_token").GetString());
        return client;
    }

    /// <summary>Mints the reset token the invitation email would carry and uses it.</summary>
    private async Task SetPasswordViaResetTokenAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        Assert.NotNull(user);
        var token = await userManager.GeneratePasswordResetTokenAsync(user!);
        var result = await userManager.ResetPasswordAsync(user!, token, password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(e => e.Description)));
    }

    private static async Task<List<JsonElement>> ItemsAsync(HttpClient client, string url)
    {
        var resp = await client.GetAsync(url);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var items = json.ValueKind == JsonValueKind.Array ? json : json.GetProperty("items");
        return items.EnumerateArray().ToList();
    }
}
