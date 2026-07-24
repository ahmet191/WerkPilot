using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Infrastructure.Identity;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// Tenant-customizable UI schemas: the shipped built-in serves everyone by
/// default, a tenant's grid override applies to its own users, security-
/// sensitive parts are always recomputed from the built-in, and reverting
/// restores the shipped layout.
/// </summary>
public class UiSchemaOverrideTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private const string RequesterUser = "alice@omniflow.com";
    private const string DemoPass = "Demo@12345";
    private const string Entity = "departments";

    private readonly CustomWebApplicationFactory _factory;

    public UiSchemaOverrideTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Defaults_are_served_when_the_tenant_has_no_override()
    {
        var admin = await ClientAsync(AdminUser, AdminPass);
        var schema = await admin.GetFromJsonAsync<JsonElement>($"/api/uischema/{Entity}");

        var columns = schema.GetProperty("grid").GetProperty("columns");
        Assert.Equal(3, columns.GetArrayLength());
        Assert.Equal("name", columns[0].GetProperty("dataIndex").GetString());
        Assert.Equal("/departments", schema.GetProperty("grid").GetProperty("endpoint").GetString());
    }

    [Fact]
    public async Task Override_reshapes_the_grid_and_revert_restores_the_default()
    {
        var admin = await ClientAsync(AdminUser, AdminPass);

        // Keep a single column, in the tenant's chosen shape.
        var reshaped = new
        {
            grid = new
            {
                titleKey = "departments.title",
                endpoint = "/departments",
                columns = new[]
                {
                    new { dataIndex = "name", titleKey = "departments.name", type = "string", sortable = true, filterable = false, ellipsis = false, width = (int?)200 },
                },
                actions = new[] { "add", "export", "refresh" },
                rowActions = Array.Empty<object>(),
            },
            form = new { },
            widgets = Array.Empty<object>(),
        };

        var set = await admin.PutAsJsonAsync($"/api/uischema/admin/{Entity}", new { schema = reshaped });
        Assert.Equal(HttpStatusCode.NoContent, set.StatusCode);

        var merged = await admin.GetFromJsonAsync<JsonElement>($"/api/uischema/{Entity}");
        var cols = merged.GetProperty("grid").GetProperty("columns");
        Assert.Equal(1, cols.GetArrayLength());
        Assert.Equal("name", cols[0].GetProperty("dataIndex").GetString());
        Assert.Equal(200, cols[0].GetProperty("width").GetInt32());

        // The editor reports it customized alongside the shipped default.
        var editing = await admin.GetFromJsonAsync<JsonElement>($"/api/uischema/admin/{Entity}");
        Assert.True(editing.GetProperty("isCustomized").GetBoolean());
        Assert.Equal(3, editing.GetProperty("default").GetProperty("grid").GetProperty("columns").GetArrayLength());

        // Revert.
        var remove = await admin.DeleteAsync($"/api/uischema/admin/{Entity}");
        Assert.Equal(HttpStatusCode.NoContent, remove.StatusCode);

        var reverted = await admin.GetFromJsonAsync<JsonElement>($"/api/uischema/{Entity}");
        Assert.Equal(3, reverted.GetProperty("grid").GetProperty("columns").GetArrayLength());
    }

    [Fact]
    public async Task Security_sensitive_parts_are_recomputed_from_the_builtin()
    {
        var admin = await ClientAsync(AdminUser, AdminPass);

        // A tampered override tries to repoint the endpoint and smuggle in a
        // destructive row action pointing at another resource.
        var tampered = new
        {
            grid = new
            {
                titleKey = "departments.title",
                endpoint = "/evil",
                columns = new[]
                {
                    new { dataIndex = "name", titleKey = "departments.name", type = "string", sortable = true, filterable = true, ellipsis = false, width = (int?)null },
                },
                actions = new[] { "hack" },
                rowActions = new[]
                {
                    new { labelKey = "hack", method = "DELETE", endpoint = "/users/{id}", color = "danger", actionType = "delete" },
                },
            },
            form = new { submitEndpoint = "/evil" },
            widgets = Array.Empty<object>(),
        };

        await admin.PutAsJsonAsync($"/api/uischema/admin/{Entity}", new { schema = tampered });

        var served = await admin.GetFromJsonAsync<JsonElement>($"/api/uischema/{Entity}");
        var grid = served.GetProperty("grid");

        // Endpoint, actions and row actions are the shipped values, not the override's.
        Assert.Equal("/departments", grid.GetProperty("endpoint").GetString());
        Assert.DoesNotContain("hack", grid.GetProperty("actions").EnumerateArray().Select(a => a.GetString()));
        foreach (var ra in grid.GetProperty("rowActions").EnumerateArray())
        {
            Assert.StartsWith("/departments", ra.GetProperty("endpoint").GetString());
        }
        // The form is the shipped one, so its submit endpoint is untouched.
        Assert.Equal("/departments", served.GetProperty("form").GetProperty("submitEndpoint").GetString());

        await admin.DeleteAsync($"/api/uischema/admin/{Entity}");
    }

    [Fact]
    public async Task Override_does_not_leak_to_another_tenant()
    {
        var demoAdmin = await ClientAsync(AdminUser, AdminPass);
        var oneColumn = new
        {
            grid = new
            {
                titleKey = "departments.title",
                endpoint = "/departments",
                columns = new[] { new { dataIndex = "name", titleKey = "departments.name", type = "string", sortable = true, filterable = false, ellipsis = false, width = (int?)null } },
                actions = Array.Empty<string>(),
                rowActions = Array.Empty<object>(),
            },
            form = new { },
            widgets = Array.Empty<object>(),
        };
        await demoAdmin.PutAsJsonAsync($"/api/uischema/admin/{Entity}", new { schema = oneColumn });

        // Stand up a second tenant and sign in as its admin.
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var otherEmail = $"admin.{suffix}@schema.test";
        var create = await demoAdmin.PostAsJsonAsync("/api/platform/tenants", new
        {
            name = $"Schema {suffix}",
            domain = $"schema-{suffix}.werkpilot.local",
            adminEmail = otherEmail,
            adminName = "Schema Admin"
        });
        create.EnsureSuccessStatusCode();
        await SetPasswordAsync(otherEmail, "SchemaPass@123");
        var otherAdmin = await ClientAsync(otherEmail, "SchemaPass@123");

        var otherSchema = await otherAdmin.GetFromJsonAsync<JsonElement>($"/api/uischema/{Entity}");
        Assert.Equal(3, otherSchema.GetProperty("grid").GetProperty("columns").GetArrayLength()); // untouched

        await demoAdmin.DeleteAsync($"/api/uischema/admin/{Entity}");
    }

    [Fact]
    public async Task Editing_requires_the_uischema_permission()
    {
        var requester = await ClientAsync(RequesterUser, DemoPass);

        Assert.Equal(HttpStatusCode.Forbidden, (await requester.GetAsync($"/api/uischema/admin/{Entity}")).StatusCode);

        var set = await requester.PutAsJsonAsync($"/api/uischema/admin/{Entity}",
            new { schema = new { grid = new { columns = Array.Empty<object>() } } });
        Assert.Equal(HttpStatusCode.Forbidden, set.StatusCode);

        // Reading the schema to render a screen stays open to any signed-in user.
        Assert.Equal(HttpStatusCode.OK, (await requester.GetAsync($"/api/uischema/{Entity}")).StatusCode);
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
