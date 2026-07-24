using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Infrastructure.Identity;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// Tenant-customizable UI text: the shipped defaults serve anonymous callers,
/// a tenant's overrides are merged for its own users only, and reverting
/// restores the default.
/// </summary>
public class TranslationOverrideTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private const string RequesterUser = "alice@omniflow.com";
    private const string DemoPass = "Demo@12345";
    private const string ProbeKey = "sidebar.tickets";

    private readonly CustomWebApplicationFactory _factory;

    public TranslationOverrideTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Anonymous_caller_gets_the_shipped_defaults()
    {
        var anonymous = _factory.CreateClient();
        var dict = await anonymous.GetFromJsonAsync<Dictionary<string, string>>("/api/localization/en");

        Assert.NotNull(dict);
        Assert.Equal("Tickets", dict![ProbeKey]);
    }

    [Fact]
    public async Task Override_applies_to_the_tenant_and_revert_restores_the_default()
    {
        var admin = await ClientAsync(AdminUser, AdminPass);

        // Customize.
        var set = await admin.PutAsJsonAsync($"/api/localization/admin/en/{ProbeKey}", new { value = "Requests" });
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        var merged = await admin.GetFromJsonAsync<Dictionary<string, string>>("/api/localization/en");
        Assert.Equal("Requests", merged![ProbeKey]);

        // The editor shows it as customized alongside the shipped default.
        var editing = await admin.GetFromJsonAsync<JsonElement>("/api/localization/admin/en");
        var row = editing.GetProperty("items").EnumerateArray()
            .First(i => i.GetProperty("key").GetString() == ProbeKey);
        Assert.True(row.GetProperty("isCustomized").GetBoolean());
        Assert.Equal("Tickets", row.GetProperty("defaultValue").GetString());
        Assert.Equal("Requests", row.GetProperty("overrideValue").GetString());

        // Revert.
        var remove = await admin.DeleteAsync($"/api/localization/admin/en/{ProbeKey}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        var reverted = await admin.GetFromJsonAsync<Dictionary<string, string>>("/api/localization/en");
        Assert.Equal("Tickets", reverted![ProbeKey]);
    }

    [Fact]
    public async Task Override_does_not_leak_to_another_tenant()
    {
        var demoAdmin = await ClientAsync(AdminUser, AdminPass);
        await demoAdmin.PutAsJsonAsync($"/api/localization/admin/en/{ProbeKey}", new { value = "DemoOnlyWording" });

        // Stand up a second tenant and sign in as its admin.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var otherEmail = $"admin.{suffix}@lang.test";
        var create = await demoAdmin.PostAsJsonAsync("/api/platform/tenants", new
        {
            name = $"Lang {suffix}",
            domain = $"lang-{suffix}.werkpilot.local",
            adminEmail = otherEmail,
            adminName = "Lang Admin"
        });
        create.EnsureSuccessStatusCode();
        await SetPasswordAsync(otherEmail, "LangPass@123");
        var otherAdmin = await ClientAsync(otherEmail, "LangPass@123");

        var otherDict = await otherAdmin.GetFromJsonAsync<Dictionary<string, string>>("/api/localization/en");
        Assert.Equal("Tickets", otherDict![ProbeKey]); // untouched by the demo tenant's wording

        // Clean up so the shared demo tenant is left as found.
        await demoAdmin.DeleteAsync($"/api/localization/admin/en/{ProbeKey}");
    }

    [Fact]
    public async Task Editing_requires_the_translations_permission()
    {
        var requester = await ClientAsync(RequesterUser, DemoPass);

        Assert.Equal(HttpStatusCode.Forbidden, (await requester.GetAsync("/api/localization/admin/en")).StatusCode);

        var set = await requester.PutAsJsonAsync($"/api/localization/admin/en/{ProbeKey}", new { value = "nope" });
        Assert.Equal(HttpStatusCode.Forbidden, set.StatusCode);

        // Reading the merged dictionary stays open to everyone.
        Assert.Equal(HttpStatusCode.OK, (await requester.GetAsync("/api/localization/en")).StatusCode);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<HttpClient> ClientAsync(string username, string password)
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid email profile roles offline_access",
        }));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("access_token").GetString());
        return client;
    }

    private async Task SetPasswordAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        var token = await userManager.GeneratePasswordResetTokenAsync(user!);
        await userManager.ResetPasswordAsync(user!, token, password);
    }
}
