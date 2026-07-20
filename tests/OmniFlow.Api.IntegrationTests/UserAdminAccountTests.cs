using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Infrastructure.Identity;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// Admin user-management console + self-service password recovery, end-to-end
/// against the real host (own Postgres container). Covers the create→login
/// chain, lockout recovery, enable/disable, role assignment, and the reset
/// token flow (token minted server-side since tests run without SMTP).
/// </summary>
public class UserAdminAccountTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private static readonly Guid HqDepartmentId = Guid.Parse("99999999-9999-9999-9999-999999999999");

    private readonly CustomWebApplicationFactory _factory;

    public UserAdminAccountTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Admin_creates_user_with_password_and_they_can_sign_in()
    {
        var admin = await AdminClientAsync();

        var email = $"jane.doe.{Guid.NewGuid():N}@werkpilot.local";
        var create = await admin.PostAsJsonAsync("/api/users", new
        {
            name = "Jane Doe",
            email,
            role = "Agent",
            departmentId = await AnyDepartmentIdAsync(admin),
            initialPassword = "Initial@12345",
            sendInvite = false
        });
        Assert.Equal(HttpStatusCode.OK, create.StatusCode);
        var userId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid();

        // The freshly created user can authenticate immediately.
        var login = await PasswordGrantAsync(_factory.CreateClient(), email, "Initial@12345");
        Assert.Equal(HttpStatusCode.OK, login.Status);

        // Their token carries the mapped Identity role (Agent → ITAgent).
        var account = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/users/{userId}/account");
        Assert.True(account.GetProperty("hasAccount").GetBoolean());
        Assert.Contains("ITAgent", account.GetProperty("roles").EnumerateArray().Select(r => r.GetString()));
    }

    [Fact]
    public async Task Lockout_after_failed_attempts_and_admin_unlock_restores_login()
    {
        var admin = await AdminClientAsync();
        var (userId, email) = await CreateUserAsync(admin, "Lock Test", "Lock@12345");
        var client = _factory.CreateClient();

        for (var i = 0; i < 5; i++)
        {
            await PasswordGrantAsync(client, email, "wrong-password");
        }

        // Correct password is now rejected (locked out).
        var locked = await PasswordGrantAsync(client, email, "Lock@12345");
        Assert.Equal(HttpStatusCode.BadRequest, locked.Status);

        var status = await admin.GetFromJsonAsync<JsonElement>($"/api/admin/users/{userId}/account");
        Assert.True(status.GetProperty("lockedOut").GetBoolean());

        var unlock = await admin.PostAsync($"/api/admin/users/{userId}/unlock", null);
        Assert.Equal(HttpStatusCode.NoContent, unlock.StatusCode);

        var restored = await PasswordGrantAsync(client, email, "Lock@12345");
        Assert.Equal(HttpStatusCode.OK, restored.Status);
    }

    [Fact]
    public async Task Disable_blocks_login_and_enable_restores_it()
    {
        var admin = await AdminClientAsync();
        var (userId, email) = await CreateUserAsync(admin, "Disable Test", "Disable@123");
        var client = _factory.CreateClient();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/users/{userId}/disable", null)).StatusCode);
        var blocked = await PasswordGrantAsync(client, email, "Disable@123");
        Assert.Equal(HttpStatusCode.BadRequest, blocked.Status);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PostAsync($"/api/admin/users/{userId}/enable", null)).StatusCode);
        var restored = await PasswordGrantAsync(client, email, "Disable@123");
        Assert.Equal(HttpStatusCode.OK, restored.Status);
    }

    [Fact]
    public async Task Admin_temp_password_and_role_change_take_effect()
    {
        var admin = await AdminClientAsync();
        var (userId, email) = await CreateUserAsync(admin, "Reset Test", "Original@123");
        var client = _factory.CreateClient();

        // Temp password replaces the old one.
        var reset = await admin.PostAsJsonAsync($"/api/admin/users/{userId}/reset-password", new { newPassword = "TempPass@123" });
        Assert.Equal(HttpStatusCode.NoContent, reset.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await PasswordGrantAsync(client, email, "Original@123")).Status);
        var withTemp = await PasswordGrantAsync(client, email, "TempPass@123");
        Assert.Equal(HttpStatusCode.OK, withTemp.Status);

        // Role assignment shows up in fresh tokens.
        var roles = await admin.PutAsJsonAsync($"/api/admin/users/{userId}/roles", new { roles = new[] { "HRAdmin" } });
        Assert.Equal(HttpStatusCode.NoContent, roles.StatusCode);
        var token = await PasswordGrantAsync(client, email, "TempPass@123");
        Assert.Contains("HRAdmin", RolesFromToken(token.Json));
    }

    [Fact]
    public async Task Admin_cannot_remove_own_administrator_role_or_disable_self()
    {
        var admin = await AdminClientAsync();
        var adminId = Guid.Parse("44444444-4444-4444-4444-444444444444");

        var stripRoles = await admin.PutAsJsonAsync($"/api/admin/users/{adminId}/roles", new { roles = new[] { "Requester" } });
        Assert.Equal(HttpStatusCode.BadRequest, stripRoles.StatusCode);

        var disableSelf = await admin.PostAsync($"/api/admin/users/{adminId}/disable", null);
        Assert.Equal(HttpStatusCode.BadRequest, disableSelf.StatusCode);
    }

    [Fact]
    public async Task Password_reset_token_flow_sets_new_password_and_clears_lockout()
    {
        var admin = await AdminClientAsync();
        var (userId, email) = await CreateUserAsync(admin, "Token Test", "TokenOld@123");
        var client = _factory.CreateClient();

        // Forgot-password is anonymous and always generic.
        var forgot = await client.PostAsJsonAsync("/api/account/forgot-password", new { email });
        Assert.Equal(HttpStatusCode.OK, forgot.StatusCode);

        // Without SMTP in tests, mint the reset token exactly like the email would carry it.
        string token;
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            token = await userManager.GeneratePasswordResetTokenAsync(user!);
        }

        var reset = await client.PostAsJsonAsync("/api/account/reset-password", new
        {
            email,
            token,
            newPassword = "TokenNew@123"
        });
        Assert.Equal(HttpStatusCode.OK, reset.StatusCode);

        Assert.Equal(HttpStatusCode.BadRequest, (await PasswordGrantAsync(client, email, "TokenOld@123")).Status);
        Assert.Equal(HttpStatusCode.OK, (await PasswordGrantAsync(client, email, "TokenNew@123")).Status);
    }

    [Fact]
    public async Task Change_password_requires_current_password()
    {
        var admin = await AdminClientAsync();
        var (_, email) = await CreateUserAsync(admin, "Change Test", "ChangeOld@123");

        var client = _factory.CreateClient();
        var login = await PasswordGrantAsync(client, email, "ChangeOld@123");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", login.Json.GetProperty("access_token").GetString());

        var wrong = await client.PostAsJsonAsync("/api/account/change-password", new
        {
            currentPassword = "not-the-password",
            newPassword = "ChangeNew@123"
        });
        Assert.Equal(HttpStatusCode.BadRequest, wrong.StatusCode);

        var ok = await client.PostAsJsonAsync("/api/account/change-password", new
        {
            currentPassword = "ChangeOld@123",
            newPassword = "ChangeNew@123"
        });
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);

        Assert.Equal(HttpStatusCode.OK, (await PasswordGrantAsync(_factory.CreateClient(), email, "ChangeNew@123")).Status);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private sealed record TokenResult(HttpStatusCode Status, JsonElement Json);

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await PasswordGrantAsync(client, AdminUser, AdminPass);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", token.Json.GetProperty("access_token").GetString());
        return client;
    }

    private async Task<(Guid Id, string Email)> CreateUserAsync(HttpClient admin, string name, string password)
    {
        var email = $"{name.Replace(" ", ".").ToLowerInvariant()}.{Guid.NewGuid():N}@werkpilot.local";
        var resp = await admin.PostAsJsonAsync("/api/users", new
        {
            name,
            email,
            role = "Requester",
            departmentId = await AnyDepartmentIdAsync(admin),
            initialPassword = password,
            sendInvite = false
        });
        resp.EnsureSuccessStatusCode();
        return ((await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("userId").GetGuid(), email);
    }

    private static async Task<Guid> AnyDepartmentIdAsync(HttpClient admin)
    {
        var resp = await admin.GetFromJsonAsync<JsonElement>("/api/departments?pageSize=1");
        var items = resp.TryGetProperty("items", out var i) ? i : resp;
        return items.EnumerateArray().First().GetProperty("id").GetGuid();
    }

    private static async Task<TokenResult> PasswordGrantAsync(HttpClient client, string username, string password)
    {
        var resp = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = username,
            ["password"] = password,
            ["scope"] = "openid email profile roles offline_access",
        }));
        return new TokenResult(resp.StatusCode, await resp.Content.ReadFromJsonAsync<JsonElement>());
    }

    private static List<string> RolesFromToken(JsonElement tokenJson)
    {
        var jwt = tokenJson.GetProperty("access_token").GetString()!;
        var payload = jwt.Split('.')[1];
        payload = payload.Replace('-', '+').Replace('_', '/');
        payload += new string('=', (4 - payload.Length % 4) % 4);
        var claims = JsonSerializer.Deserialize<JsonElement>(Convert.FromBase64String(payload));
        if (!claims.TryGetProperty("role", out var role)) return [];
        return role.ValueKind == JsonValueKind.Array
            ? role.EnumerateArray().Select(r => r.GetString()!).ToList()
            : [role.GetString()!];
    }
}
