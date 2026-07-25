using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Infrastructure.Persistence;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// Phase 2-5 feature hardening: AI triage persists a prediction on ticket
/// creation (F0.1), the admin-config surfaces are permission-gated (Phase A),
/// and the HR skill CRUD round-trips owner-scoped (Phase C).
/// </summary>
public class NewFeatureTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private const string RequesterUser = "alice@omniflow.com";
    private const string DemoPass = "Demo@12345";

    private static readonly Guid ItTemplateId = Guid.Parse("2e3e5c9b-6dbb-4171-aa7a-8cf0c74fb1d2");
    private static readonly Guid RequesterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ItServiceDeskId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private readonly CustomWebApplicationFactory _factory;

    public NewFeatureTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Creating_a_ticket_persists_an_AI_triage_prediction()
    {
        var admin = await ClientAsync(AdminUser, AdminPass);
        var ticketId = await CreateTicketAsync(admin);

        // The triage handler runs in-process during ticket creation, so by the
        // time the call returns a prediction row exists for the new ticket.
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OmniFlowDbContext>();
        var prediction = await db.TicketAiPredictions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.TicketId == ticketId);

        Assert.NotNull(prediction);
        Assert.False(string.IsNullOrWhiteSpace(prediction!.PredictedCategory));
        Assert.False(string.IsNullOrWhiteSpace(prediction.PredictedPriority));
    }

    [Fact]
    public async Task Admin_config_endpoints_require_the_manage_permission()
    {
        var requester = await ClientAsync(RequesterUser, DemoPass);
        var admin = await ClientAsync(AdminUser, AdminPass);

        // A Requester holds no sla/users/templates manage permission.
        var slaBody = new { name = "T", priority = "Medium", responseTimeMinutes = 60, resolutionTimeHours = 24, escalateAfterPercent = 80, notifyManagerOnBreach = true, isActive = true };
        var studioBody = new { name = "T Dept", code = "TST", departmentType = "Custom", colorHex = "#123456", isActive = true };
        var formBody = new { serviceDeskId = ItServiceDeskId, formTitle = "T", formDescription = "d", fieldsJson = "[]", isActive = true };

        Assert.Equal(HttpStatusCode.Forbidden, (await requester.PostAsJsonAsync("/api/slas/policies", slaBody)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.PostAsJsonAsync("/api/departments/studio", studioBody)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await requester.PostAsJsonAsync("/api/forms/schemas", formBody)).StatusCode);

        // The Administrator (super-role) is allowed through.
        Assert.NotEqual(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/slas/policies", slaBody)).StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/departments/studio", studioBody)).StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, (await admin.PostAsJsonAsync("/api/forms/schemas", formBody)).StatusCode);

        // Reading a schema to render a screen stays open to any signed-in user.
        Assert.Equal(HttpStatusCode.OK, (await requester.GetAsync("/api/slas/policies")).StatusCode);
    }

    [Fact]
    public async Task Validation_rejects_malformed_admin_config_commands()
    {
        // The admin clears authorization, so a 400 here proves the FluentValidation
        // pipeline (not RBAC) is rejecting the malformed payload for each of the
        // Phase 2-5 commands.
        var admin = await ClientAsync(AdminUser, AdminPass);

        // SLA: blank name + an out-of-range escalation threshold.
        var badSla = new { name = "", priority = "Nonsense", responseTimeMinutes = 0, resolutionTimeHours = 0, escalateAfterPercent = 250, isActive = true };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/slas/policies", badSla)).StatusCode);

        // Custom department: no name, and a colour that is not a hex swatch.
        var badDept = new { name = "", code = "X", departmentType = "Custom", colorHex = "not-a-colour", isActive = true };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/departments/studio", badDept)).StatusCode);

        // Form schema: no desk selected and malformed field JSON.
        var badForm = new { serviceDeskId = Guid.Empty, formTitle = "", formDescription = "d", fieldsJson = "{ not json", isActive = true };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/forms/schemas", badForm)).StatusCode);

        // Goal: blank title and a quarter that is not Q1-Q4/Annual.
        var badGoal = new { title = "", quarter = "Q9", year = 2026, targetValue = 100, currentValue = 0, unit = "Percentage", status = "InProgress" };
        Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsJsonAsync("/api/hr/goals", badGoal)).StatusCode);
    }

    [Fact]
    public async Task Skill_create_and_delete_round_trips_for_the_owner()
    {
        var admin = await ClientAsync(AdminUser, AdminPass);
        var name = $"Probe Skill {Guid.NewGuid():N}";

        var create = await admin.PostAsJsonAsync("/api/hr/skills", new { skillName = name, category = "Technical", proficiencyLevel = 4 });
        create.EnsureSuccessStatusCode();
        var id = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetGuid();

        var afterAdd = await admin.GetFromJsonAsync<JsonElement>("/api/hr/skills-certificates");
        Assert.Contains(afterAdd.GetProperty("skills").EnumerateArray(), s => s.GetProperty("skillName").GetString() == name);

        var delete = await admin.DeleteAsync($"/api/hr/skills/{id}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var afterDelete = await admin.GetFromJsonAsync<JsonElement>("/api/hr/skills-certificates");
        Assert.DoesNotContain(afterDelete.GetProperty("skills").EnumerateArray(), s => s.GetProperty("skillName").GetString() == name);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<Guid> CreateTicketAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/tickets", new
        {
            templateId = ItTemplateId,
            requesterId = RequesterId,
            dynamicData = new Dictionary<string, object>
            {
                ["Title"] = "AI triage probe ticket",
                ["Description"] = "VPN gateway unreachable from the branch office",
                ["Type"] = "Incident",
                ["Priority"] = "High",
                ["Impact"] = "High",
                ["Urgency"] = "High",
            },
        });
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("ticketId").GetGuid();
    }

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
