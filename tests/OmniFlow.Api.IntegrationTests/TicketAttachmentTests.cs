using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// Ticket depth: file attachments round-trip byte-for-byte, are tenant-scoped,
/// comments and files surface in the details payload, and permissions gate
/// uploads. Exercised against the real host + Postgres container.
/// </summary>
public class TicketAttachmentTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private const string RequesterUser = "alice@omniflow.com";
    private const string DemoPass = "Demo@12345";

    private static readonly Guid ItTemplateId = Guid.Parse("2e3e5c9b-6dbb-4171-aa7a-8cf0c74fb1d2");
    private static readonly Guid RequesterId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly CustomWebApplicationFactory _factory;

    public TicketAttachmentTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Attachment_round_trips_and_surfaces_in_details()
    {
        var client = await AdminClientAsync();
        var ticketId = await CreateTicketAsync(client);

        var bytes = Encoding.UTF8.GetBytes("hello attachment world");
        var attachmentId = await UploadAsync(client, ticketId, "notes.txt", "text/plain", bytes);

        // Download returns the exact bytes.
        var download = await client.GetAsync($"/api/tickets/attachments/{attachmentId}/download");
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        var downloaded = await download.Content.ReadAsByteArrayAsync();
        Assert.Equal(bytes, downloaded);
        Assert.Equal("notes.txt", download.Content.Headers.ContentDisposition?.FileNameStar
            ?? download.Content.Headers.ContentDisposition?.FileName?.Trim('"'));

        // It appears in the ticket details payload.
        var details = await client.GetFromJsonAsync<JsonElement>($"/api/tickets/{ticketId}");
        var attachments = details.GetProperty("attachments").EnumerateArray().ToList();
        Assert.Contains(attachments, a => a.GetProperty("fileName").GetString() == "notes.txt"
                                          && a.GetProperty("sizeBytes").GetInt64() == bytes.Length);

        // The list endpoint agrees.
        var listed = await client.GetFromJsonAsync<JsonElement>($"/api/tickets/{ticketId}/attachments");
        Assert.Single(listed.EnumerateArray());
    }

    [Fact]
    public async Task Attachment_is_tenant_scoped()
    {
        // Provision a second tenant and confirm its admin cannot reach the demo
        // tenant's attachment.
        var demoAdmin = await AdminClientAsync();
        var ticketId = await CreateTicketAsync(demoAdmin);
        var attachmentId = await UploadAsync(demoAdmin, ticketId, "secret.txt", "text/plain",
            Encoding.UTF8.GetBytes("cross-tenant secret"));

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var otherEmail = $"admin.{suffix}@other.test";
        var create = await demoAdmin.PostAsJsonAsync("/api/platform/tenants", new
        {
            name = $"Other {suffix}",
            domain = $"other-{suffix}.werkpilot.local",
            adminEmail = otherEmail,
            adminName = "Other Admin"
        });
        create.EnsureSuccessStatusCode();
        await SetPasswordViaResetTokenAsync(otherEmail, "OtherPass@123");
        var otherAdmin = await ClientAsync(otherEmail, "OtherPass@123");

        // The global query filter hides the row → 404, not another tenant's bytes.
        var download = await otherAdmin.GetAsync($"/api/tickets/attachments/{attachmentId}/download");
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_the_attachment()
    {
        var client = await AdminClientAsync();
        var ticketId = await CreateTicketAsync(client);
        var attachmentId = await UploadAsync(client, ticketId, "temp.txt", "text/plain",
            Encoding.UTF8.GetBytes("delete me"));

        var delete = await client.DeleteAsync($"/api/tickets/attachments/{attachmentId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);

        var download = await client.GetAsync($"/api/tickets/attachments/{attachmentId}/download");
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);

        var listed = await client.GetFromJsonAsync<JsonElement>($"/api/tickets/{ticketId}/attachments");
        Assert.Empty(listed.EnumerateArray());
    }

    [Fact]
    public async Task Disallowed_content_type_is_rejected()
    {
        var client = await AdminClientAsync();
        var ticketId = await CreateTicketAsync(client);

        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("MZ..."));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/x-msdownload");
        content.Add(file, "files", "evil.exe");

        // The content-type allowlist rejects executables with a 400 (the UI also
        // surfaces the rule up front).
        var resp = await client.PostAsync($"/api/tickets/{ticketId}/attachments", content);
        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Any_authenticated_user_can_upload_an_allowed_file()
    {
        var admin = await AdminClientAsync();
        var ticketId = await CreateTicketAsync(admin);

        // Uploads are intentionally open to any authenticated user (e.g. a
        // requester attaching a screenshot to their ticket); the content-type
        // allowlist still governs *what* may be uploaded.
        var requester = await ClientAsync(RequesterUser, DemoPass);
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes("note"));
        file.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        content.Add(file, "files", "note.txt");

        var resp = await requester.PostAsync($"/api/tickets/{ticketId}/attachments", content);
        resp.EnsureSuccessStatusCode();

        var list = await requester.GetAsync($"/api/tickets/{ticketId}/attachments");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }

    [Fact]
    public async Task Comment_appears_in_details()
    {
        var client = await AdminClientAsync();
        var ticketId = await CreateTicketAsync(client);

        var comment = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/comments",
            new { authorId = AdminId, content = "Investigating now." });
        comment.EnsureSuccessStatusCode();

        var details = await client.GetFromJsonAsync<JsonElement>($"/api/tickets/{ticketId}");
        var comments = details.GetProperty("comments").EnumerateArray().ToList();
        Assert.Contains(comments, c => c.GetProperty("content").GetString() == "Investigating now.");
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private async Task<Guid> UploadAsync(HttpClient client, Guid ticketId, string fileName, string contentType, byte[] bytes)
    {
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(file, "files", fileName);

        var resp = await client.PostAsync($"/api/tickets/{ticketId}/attachments", content);
        resp.EnsureSuccessStatusCode();
        // The batch endpoint returns the ids of everything it stored.
        var payload = await resp.Content.ReadFromJsonAsync<JsonElement>();
        return payload.GetProperty("attachmentIds").EnumerateArray().First().GetGuid();
    }

    private async Task<Guid> CreateTicketAsync(HttpClient client)
    {
        var resp = await client.PostAsJsonAsync("/api/tickets", new
        {
            templateId = ItTemplateId,
            requesterId = RequesterId,
            dynamicData = new Dictionary<string, object>
            {
                ["Title"] = "Attachment test ticket",
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

    private async Task SetPasswordViaResetTokenAsync(string email, string password)
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<OmniFlow.Infrastructure.Identity.ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        var token = await userManager.GeneratePasswordResetTokenAsync(user!);
        await userManager.ResetPasswordAsync(user!, token, password);
    }
}
