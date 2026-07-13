using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// End-to-end critical paths against the real host + a Postgres container.
/// One container/host is shared across all tests in this class.
/// </summary>
public class CriticalPathTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    // Stable seed identities (see SeedConstants / IdentitySeedData).
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private static readonly Guid ItTemplateId = Guid.Parse("2e3e5c9b-6dbb-4171-aa7a-8cf0c74fb1d2");
    private static readonly Guid RequesterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AgentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    public CriticalPathTests(CustomWebApplicationFactory factory) => _factory = factory;

    private async Task<HttpClient> AuthedClientAsync()
    {
        var client = _factory.CreateClient();
        var token = await GetTokenAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> GetTokenAsync(HttpClient client)
    {
        var form = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = AdminUser,
            ["password"] = AdminPass,
            ["scope"] = "openid email profile roles offline_access",
        });
        var resp = await client.PostAsync("/connect/token", form);
        resp.EnsureSuccessStatusCode();
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("access_token").GetString()!;
    }

    // ── Auth ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Anonymous_request_to_protected_endpoint_is_401()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/users");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Login_then_authenticated_request_succeeds()
    {
        var client = await AuthedClientAsync();
        var resp = await client.GetAsync("/api/users?current=1&pageSize=10");
        resp.EnsureSuccessStatusCode();

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("totalCount").GetInt32() >= 1);
    }

    // ── Validation → ProblemDetails ─────────────────────────────────────

    [Fact]
    public async Task Invalid_ticket_returns_400_problem_details()
    {
        var client = await AuthedClientAsync();
        var resp = await client.PostAsJsonAsync("/api/tickets", new
        {
            templateId = Guid.Empty,
            requesterId = Guid.Empty,
            dynamicData = new { },
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("api.error.validation", problem.GetProperty("messageKey").GetString());
        Assert.True(problem.TryGetProperty("errors", out _));
    }

    // ── Soft delete ─────────────────────────────────────────────────────

    [Fact]
    public async Task Deleted_department_disappears_from_list()
    {
        var client = await AuthedClientAsync();

        var createResp = await client.PostAsJsonAsync("/api/departments", new { name = "Temp Dept For Deletion" });
        createResp.EnsureSuccessStatusCode();

        Guid deptId = await FindDepartmentIdAsync(client, "Temp Dept For Deletion");
        Assert.NotEqual(Guid.Empty, deptId);

        var delResp = await client.DeleteAsync($"/api/departments/{deptId}");
        Assert.Equal(HttpStatusCode.NoContent, delResp.StatusCode);

        // Soft-deleted → filtered out of the list.
        var afterId = await FindDepartmentIdAsync(client, "Temp Dept For Deletion");
        Assert.Equal(Guid.Empty, afterId);
    }

    private static async Task<Guid> FindDepartmentIdAsync(HttpClient client, string name)
    {
        var resp = await client.GetAsync("/api/departments?current=1&pageSize=200");
        resp.EnsureSuccessStatusCode();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        foreach (var item in body.GetProperty("items").EnumerateArray())
        {
            if (item.GetProperty("name").GetString() == name)
                return item.GetProperty("id").GetGuid();
        }
        return Guid.Empty;
    }

    // ── Full approval flow (headline feature) ───────────────────────────

    [Fact]
    public async Task Ticket_approval_flow_auto_transitions_to_resolved()
    {
        var client = await AuthedClientAsync();

        // Create a ticket. DynamicData is a dictionary so its keys serialize
        // verbatim (PascalCase) — the template's required field keys are
        // compared case-sensitively. (Anonymous-object props would be
        // camelCased by the Web JSON defaults and miss the required fields.)
        var createResp = await client.PostAsJsonAsync("/api/tickets", new
        {
            templateId = ItTemplateId,
            requesterId = RequesterId,
            dynamicData = new Dictionary<string, object>
            {
                ["Title"] = "Integration approval test",
                ["Description"] = "x",
                ["Type"] = "Incident",
                ["Priority"] = "High",
                ["Impact"] = "Medium",
                ["Urgency"] = "Medium",
            },
        });
        await EnsureOk(createResp, "create ticket");
        var ticketId = (await createResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ticketId").GetGuid();

        // Drive to WaitingForApproval.
        await Transition(client, ticketId, "StartWork", AgentId);
        await Transition(client, ticketId, "RequestApproval", AgentId);

        // An approval request should now exist.
        var approvalsResp = await client.GetAsync($"/api/approvals/entity/Ticket/{ticketId}");
        approvalsResp.EnsureSuccessStatusCode();
        var approvals = await approvalsResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(approvals.GetArrayLength() >= 1);
        var approvalId = approvals[0].GetProperty("id").GetGuid();

        // A direct user-fired transition while pending approval is forbidden.
        var directResp = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/transition",
            new { trigger = "Approve", userId = AgentId });
        Assert.Equal(HttpStatusCode.Forbidden, directResp.StatusCode);

        // Decide the approval → the consumer auto-transitions the ticket.
        var decideResp = await client.PostAsJsonAsync($"/api/approvals/{approvalId}/decide",
            new { approve = true, comment = "ok" });
        decideResp.EnsureSuccessStatusCode();

        // The consumer runs asynchronously; poll briefly for the state change.
        var state = await PollTicketStateAsync(client, ticketId, "Resolved", TimeSpan.FromSeconds(10));
        Assert.Equal("Resolved", state);
    }

    private static async Task Transition(HttpClient client, Guid ticketId, string trigger, Guid userId)
    {
        var resp = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/transition", new { trigger, userId });
        await EnsureOk(resp, $"transition {trigger}");
    }

    private static async Task EnsureOk(HttpResponseMessage resp, string what)
    {
        if (!resp.IsSuccessStatusCode)
        {
            var body = await resp.Content.ReadAsStringAsync();
            Assert.Fail($"{what} failed: {(int)resp.StatusCode} {resp.StatusCode}\n{body}");
        }
    }

    private static async Task<string?> PollTicketStateAsync(HttpClient client, Guid ticketId, string expected, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        string? state = null;
        while (DateTime.UtcNow < deadline)
        {
            var resp = await client.GetAsync($"/api/tickets/{ticketId}");
            if (resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
                state = body.GetProperty("currentState").GetString();
                if (state == expected) return state;
            }
            await Task.Delay(500);
        }
        return state;
    }
}
