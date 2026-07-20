using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// The validated workflow endpoints behind the visual designer: the palette is
/// exposed, bad workflows are rejected with reasons, and a saved workflow
/// actually drives ticket transitions.
/// </summary>
public class WorkflowDesignerTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private const string RequesterUser = "alice@omniflow.com";
    private const string DemoPass = "Demo@12345";

    private static readonly Guid ItTemplateId = Guid.Parse("2e3e5c9b-6dbb-4171-aa7a-8cf0c74fb1d2");
    private static readonly Guid RequesterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AgentId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private readonly CustomWebApplicationFactory _factory;

    public WorkflowDesignerTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Get_workflow_exposes_the_palette()
    {
        var admin = await AdminClientAsync();
        var res = await admin.GetFromJsonAsync<JsonElement>($"/api/templates/{ItTemplateId}/workflow");

        var states = res.GetProperty("states").EnumerateArray().Select(s => s.GetString()).ToList();
        Assert.Contains("New", states);
        Assert.Contains("WaitingForApproval", states);
        Assert.True(res.GetProperty("roles").GetArrayLength() > 0);
        Assert.True(res.GetProperty("workflow").GetProperty("transitions").GetArrayLength() > 0);
    }

    [Fact]
    public async Task Invalid_workflow_is_rejected_with_reasons()
    {
        var admin = await AdminClientAsync();

        // Duplicate trigger from the same state.
        var resp = await admin.PutAsJsonAsync($"/api/templates/{ItTemplateId}/workflow", new
        {
            transitions = new[]
            {
                new { fromState = "New", toState = "InProgress", trigger = "Go", requiredRole = (string?)null, approvalPolicy = (object?)null },
                new { fromState = "New", toState = "Resolved", trigger = "Go", requiredRole = (string?)null, approvalPolicy = (object?)null },
            }
        });

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        var problem = await resp.Content.ReadFromJsonAsync<JsonElement>();
        var errors = problem.GetProperty("errors").GetProperty("workflow").EnumerateArray().Select(e => e.GetString());
        Assert.Contains(errors, e => e!.Contains("already used from state"));
    }

    [Fact]
    public async Task Self_transition_is_rejected()
    {
        var admin = await AdminClientAsync();
        var resp = await admin.PutAsJsonAsync($"/api/templates/{ItTemplateId}/workflow", new
        {
            transitions = new[]
            {
                new { fromState = "InProgress", toState = "InProgress", trigger = "Loop", requiredRole = (string?)null, approvalPolicy = (object?)null },
            }
        });
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Saved_workflow_drives_ticket_transitions()
    {
        // A dedicated template keeps this test from disturbing the shared demo
        // template other tests rely on.
        var admin = await AdminClientAsync();
        var templateId = await CloneTemplateAsync(admin);

        // A simple linear workflow with a custom trigger name.
        var save = await admin.PutAsJsonAsync($"/api/templates/{templateId}/workflow", new
        {
            transitions = new[]
            {
                new { fromState = "New", toState = "InProgress", trigger = "Begin", requiredRole = "Agent", approvalPolicy = (object?)null },
                new { fromState = "InProgress", toState = "Resolved", trigger = "Finish", requiredRole = "Agent", approvalPolicy = (object?)null },
                new { fromState = "Resolved", toState = "Closed", trigger = "Archive", requiredRole = (string?)null, approvalPolicy = (object?)null },
            }
        });
        Assert.Equal(HttpStatusCode.NoContent, save.StatusCode);

        var ticketId = await CreateTicketAsync(admin, templateId);

        // The ticket's available triggers reflect the edited workflow.
        var details = await admin.GetFromJsonAsync<JsonElement>($"/api/tickets/{ticketId}");
        var triggers = details.GetProperty("availableTriggers").EnumerateArray().Select(x => x.GetString()).ToList();
        Assert.Contains("Begin", triggers);

        // And the ticket can actually be driven through the new path.
        var transition = await admin.PostAsJsonAsync($"/api/tickets/{ticketId}/transition",
            new { trigger = "Begin", userId = AgentId });
        Assert.Equal(HttpStatusCode.OK, transition.StatusCode);

        var after = await admin.GetFromJsonAsync<JsonElement>($"/api/tickets/{ticketId}");
        Assert.Equal("InProgress", after.GetProperty("currentState").GetString());
    }

    [Fact]
    public async Task Requester_cannot_edit_the_workflow()
    {
        var requester = await ClientAsync(RequesterUser, DemoPass);
        var resp = await requester.GetAsync($"/api/templates/{ItTemplateId}/workflow");
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<Guid> CloneTemplateAsync(HttpClient admin)
    {
        var template = await admin.GetFromJsonAsync<JsonElement>($"/api/templates/{ItTemplateId}");
        // Re-post the template body under a new id (Create assigns one when empty).
        var body = JsonSerializer.Deserialize<Dictionary<string, object>>(template.GetRawText())!;
        body.Remove("id");
        var resp = await admin.PostAsJsonAsync("/api/templates", body);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("templateId").GetGuid();
    }

    private async Task<Guid> CreateTicketAsync(HttpClient admin, Guid templateId)
    {
        var resp = await admin.PostAsJsonAsync("/api/tickets", new
        {
            templateId,
            requesterId = RequesterId,
            dynamicData = new Dictionary<string, object>
            {
                ["Title"] = "Workflow driven ticket",
                ["Description"] = "x",
                ["Type"] = "Incident",
                ["Priority"] = "Medium",
                ["Impact"] = "Low",
                ["Urgency"] = "Low",
            },
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ticketId").GetGuid();
    }

    private Task<HttpClient> AdminClientAsync() => ClientAsync(AdminUser, AdminPass);

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
}
