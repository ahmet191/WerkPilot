using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// The employee directory stitches two queries together — profiles and the
/// users who have none yet. Both project into the same shape, so a change to
/// either projection can break materialization without breaking the build.
/// </summary>
public class DirectoryTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";

    private readonly CustomWebApplicationFactory _factory;

    public DirectoryTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Employees_returns_both_profiled_and_profile_less_users()
    {
        var admin = await AdminClientAsync();
        var res = await admin.GetFromJsonAsync<JsonElement>("/api/directory/employees");

        var rows = res.EnumerateArray().ToList();
        Assert.NotEmpty(rows);

        // Every row carries the full directory shape, whichever query produced it.
        foreach (var row in rows)
        {
            Assert.NotEqual(Guid.Empty, row.GetProperty("userId").GetGuid());
            Assert.False(string.IsNullOrWhiteSpace(row.GetProperty("name").GetString()));
            Assert.Equal(JsonValueKind.Array, row.GetProperty("skills").ValueKind);
            // Present even when null — a missing member would mean the two
            // projections drifted apart again.
            Assert.True(row.TryGetProperty("email", out _));
            Assert.True(row.TryGetProperty("department", out _));
        }

        // The seeded demo users have no DirectoryProfile row, so reaching them at
        // all proves the second projection materialized.
        Assert.Contains(rows, r => r.GetProperty("email").GetString() == "alice@omniflow.com");
    }

    [Fact]
    public async Task Employees_requires_authentication()
    {
        var anonymous = _factory.CreateClient();
        var res = await anonymous.GetAsync("/api/directory/employees");
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, res.StatusCode);
    }

    private async Task<HttpClient> AdminClientAsync()
    {
        var client = _factory.CreateClient();
        var resp = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = AdminUser,
            ["password"] = AdminPass,
            ["scope"] = "openid email profile roles offline_access",
        }));
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", json.GetProperty("access_token").GetString());
        return client;
    }
}
