using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// Per-tenant RBAC proven with real non-admin logins: the seeded agent,
/// HR admin and requester accounts exercise the permission matrix end to end,
/// including runtime grant/revoke and the Administrator lockout guard.
/// </summary>
public class PermissionMatrixTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private const string AgentUser = "bob@omniflow.com";      // ITAgent
    private const string RequesterUser = "alice@omniflow.com"; // Requester
    private const string DemoPass = "Demo@12345";

    private readonly CustomWebApplicationFactory _factory;

    public PermissionMatrixTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Requester_is_denied_admin_surfaces_that_admin_can_reach()
    {
        var requester = await ClientAsync(RequesterUser, DemoPass);
        var admin = await ClientAsync(AdminUser, AdminPass);

        // users.manage / auditlogs.view / permissions.manage are not in the
        // Requester's default grants.
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.GetAsync("/api/auditlogs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.GetAsync("/api/permissions/matrix")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden,
            (await requester.PostAsJsonAsync("/api/users", new { name = "X", email = "x@y.z", role = "Requester", departmentId = Guid.Empty })).StatusCode);

        // The same endpoints are open to the Administrator.
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/auditlogs")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/api/permissions/matrix")).StatusCode);
    }

    [Fact]
    public async Task Effective_permissions_reflect_the_role_of_the_caller()
    {
        var agent = await ClientAsync(AgentUser, DemoPass);
        var mine = await agent.GetFromJsonAsync<JsonElement>("/api/permissions/mine");

        var roles = mine.GetProperty("roles").EnumerateArray().Select(r => r.GetString()).ToList();
        var permissions = mine.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToList();

        Assert.Contains("ITAgent", roles);
        Assert.Contains("tickets.assign", permissions);      // granted by default
        Assert.DoesNotContain("users.manage", permissions);  // administration stays closed
        Assert.DoesNotContain("permissions.manage", permissions);
    }

    [Fact]
    public async Task Administrator_always_holds_every_permission()
    {
        var admin = await ClientAsync(AdminUser, AdminPass);
        var mine = await admin.GetFromJsonAsync<JsonElement>("/api/permissions/mine");
        var permissions = mine.GetProperty("permissions").EnumerateArray().Select(p => p.GetString()).ToList();

        // The super role is not matrix-driven: it holds the full catalog.
        Assert.Contains("permissions.manage", permissions);
        Assert.Contains("tickets.assign", permissions);
        Assert.Contains("auditlogs.view", permissions);

        // ...and it cannot be edited into a corner.
        var attempt = await admin.PutAsJsonAsync("/api/permissions/matrix/Administrator",
            new { permissions = new[] { "tickets.view" } });
        Assert.Equal(HttpStatusCode.BadRequest, attempt.StatusCode);
    }

    [Fact]
    public async Task Granting_and_revoking_a_permission_takes_effect_immediately()
    {
        var admin = await ClientAsync(AdminUser, AdminPass);
        var agent = await ClientAsync(AgentUser, DemoPass);

        // Baseline: the agent cannot read audit logs.
        Assert.Equal(HttpStatusCode.Forbidden, (await agent.GetAsync("/api/auditlogs")).StatusCode);

        var original = await CurrentGrantsAsync(admin, "ITAgent");

        // Grant auditlogs.view at runtime — no restart, no re-login.
        await SetGrantsAsync(admin, "ITAgent", [.. original, "auditlogs.view"]);
        Assert.Equal(HttpStatusCode.OK, (await agent.GetAsync("/api/auditlogs")).StatusCode);

        // Revoke it again and the door closes on the very next request.
        await SetGrantsAsync(admin, "ITAgent", original);
        Assert.Equal(HttpStatusCode.Forbidden, (await agent.GetAsync("/api/auditlogs")).StatusCode);
    }

    [Fact]
    public async Task Unknown_permission_keys_are_rejected()
    {
        var admin = await ClientAsync(AdminUser, AdminPass);
        var resp = await admin.PutAsJsonAsync("/api/permissions/matrix/ITAgent",
            new { permissions = new[] { "tickets.view", "not.a.real.permission" } });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Non_admin_cannot_edit_the_matrix()
    {
        var agent = await ClientAsync(AgentUser, DemoPass);
        var resp = await agent.PutAsJsonAsync("/api/permissions/matrix/Requester",
            new { permissions = new[] { "users.manage" } });
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
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

    private static async Task<string[]> CurrentGrantsAsync(HttpClient admin, string role)
    {
        var matrix = await admin.GetFromJsonAsync<JsonElement>("/api/permissions/matrix");
        var roleMatrix = matrix.GetProperty("matrix");
        return roleMatrix.TryGetProperty(role, out var grants)
            ? grants.EnumerateArray().Select(g => g.GetString()!).ToArray()
            : [];
    }

    private static async Task SetGrantsAsync(HttpClient admin, string role, IEnumerable<string> permissions)
    {
        var resp = await admin.PutAsJsonAsync($"/api/permissions/matrix/{role}", new { permissions = permissions.ToArray() });
        resp.EnsureSuccessStatusCode();
    }
}
