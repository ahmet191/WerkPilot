using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Enums;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// The notification centre: every notifiable event leaves an in-app record,
/// role-addressed notifications reach every holder of that role, and one user
/// can never read another's notifications.
/// </summary>
public class NotificationTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private const string AgentUser = "bob@omniflow.com";
    private const string RequesterUser = "alice@omniflow.com";
    private const string DemoPass = "Demo@12345";

    private static readonly Guid AgentId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid RequesterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid DefaultTenantId = Guid.Parse("a0000000-0000-0000-0000-000000000001");

    private readonly CustomWebApplicationFactory _factory;

    public NotificationTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Notified_user_sees_it_in_their_centre_and_nobody_else_does()
    {
        var title = $"Direct notice {Guid.NewGuid():N}";
        await NotifyUserAsync(AgentId, NotificationKind.TicketAssigned, title, "A ticket needs your attention.", "/tickets");

        var agent = await ClientAsync(AgentUser, DemoPass);
        var agentItems = await agent.GetFromJsonAsync<JsonElement>("/api/notifications?take=50");
        Assert.Contains(agentItems.EnumerateArray(), n => n.GetProperty("title").GetString() == title);

        // Scoping: the requester must not see the agent's notification.
        var requester = await ClientAsync(RequesterUser, DemoPass);
        var requesterItems = await requester.GetFromJsonAsync<JsonElement>("/api/notifications?take=50");
        Assert.DoesNotContain(requesterItems.EnumerateArray(), n => n.GetProperty("title").GetString() == title);
    }

    [Fact]
    public async Task Role_addressed_notification_reaches_every_holder_of_that_role()
    {
        // Role-addressed approval steps used to notify nobody at all.
        var title = $"Role notice {Guid.NewGuid():N}";
        await NotifyRoleAsync("ITAgent", NotificationKind.ApprovalRequested, title, "Waiting for an IT agent.", "/approvals");

        var agent = await ClientAsync(AgentUser, DemoPass);   // holds ITAgent
        var items = await agent.GetFromJsonAsync<JsonElement>("/api/notifications?take=50");
        Assert.Contains(items.EnumerateArray(), n => n.GetProperty("title").GetString() == title);

        var requester = await ClientAsync(RequesterUser, DemoPass); // does not hold ITAgent
        var requesterItems = await requester.GetFromJsonAsync<JsonElement>("/api/notifications?take=50");
        Assert.DoesNotContain(requesterItems.EnumerateArray(), n => n.GetProperty("title").GetString() == title);
    }

    [Fact]
    public async Task Unread_count_tracks_reads()
    {
        var agent = await ClientAsync(AgentUser, DemoPass);
        await agent.PostAsync("/api/notifications/read-all", null);

        var before = await UnreadCountAsync(agent);
        Assert.Equal(0, before);

        await NotifyUserAsync(AgentId, NotificationKind.General, $"Unread probe {Guid.NewGuid():N}", "body");
        Assert.Equal(1, await UnreadCountAsync(agent));

        var items = await agent.GetFromJsonAsync<JsonElement>("/api/notifications?take=1&unreadOnly=true");
        var id = items.EnumerateArray().First().GetProperty("id").GetGuid();

        var read = await agent.PostAsync($"/api/notifications/{id}/read", null);
        Assert.Equal(HttpStatusCode.NoContent, read.StatusCode);
        Assert.Equal(0, await UnreadCountAsync(agent));
    }

    [Fact]
    public async Task Cannot_mark_another_users_notification_as_read()
    {
        var title = $"Private {Guid.NewGuid():N}";
        await NotifyUserAsync(RequesterId, NotificationKind.General, title, "for the requester only");

        var requester = await ClientAsync(RequesterUser, DemoPass);
        var items = await requester.GetFromJsonAsync<JsonElement>("/api/notifications?take=50");
        var id = items.EnumerateArray().First(n => n.GetProperty("title").GetString() == title).GetProperty("id").GetGuid();

        var agent = await ClientAsync(AgentUser, DemoPass);
        var resp = await agent.PostAsync($"/api/notifications/{id}/read", null);
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [Fact]
    public async Task Notifications_require_authentication()
    {
        var anonymous = _factory.CreateClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/notifications")).StatusCode);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    /// <summary>Dispatches through the real service, exercising the full fan-out.</summary>
    private async Task NotifyUserAsync(Guid recipientId, NotificationKind kind, string title, string body, string? link = null)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantService>().SetTenantId(DefaultTenantId);
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await notifications.NotifyUserAsync(recipientId, kind, title, body, link);
    }

    private async Task NotifyRoleAsync(string role, NotificationKind kind, string title, string body, string? link = null)
    {
        using var scope = _factory.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<ITenantService>().SetTenantId(DefaultTenantId);
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();
        await notifications.NotifyRoleAsync(role, kind, title, body, link);
    }

    private static async Task<int> UnreadCountAsync(HttpClient client)
    {
        var json = await client.GetFromJsonAsync<JsonElement>("/api/notifications/unread-count");
        return json.GetProperty("count").GetInt32();
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
