using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using OmniFlow.Infrastructure.Identity;

namespace OmniFlow.Api.IntegrationTests;

/// <summary>
/// MFA (TOTP step-up on the password grant) and SSO exchange-ticket grant,
/// end-to-end against the real host. Isolated in its own class so enabling
/// MFA on the seeded admin cannot bleed into other test classes (each class
/// gets its own Postgres container via IClassFixture).
/// </summary>
public class AuthMfaSsoTests : IClassFixture<CustomWebApplicationFactory>
{
    private const string AdminUser = "admin@werkpilot.com";
    private const string AdminPass = "Admin@123";
    private static readonly Guid AdminUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly CustomWebApplicationFactory _factory;

    public AuthMfaSsoTests(CustomWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Mfa_full_lifecycle_enroll_stepup_recovery_disable()
    {
        var client = _factory.CreateClient();

        // 1. Plain password login works while MFA is off.
        var initialToken = await PasswordGrantAsync(client);
        Assert.True(initialToken.Status == HttpStatusCode.OK,
            $"Expected OK, got {initialToken.Status}: {initialToken.Json.GetRawText()}");
        var authed = WithBearer(client, initialToken.Json);

        // 2. Enroll: rotate key, compute a live TOTP, enable.
        var setupResp = await authed.PostAsync("/api/account/mfa/setup", null);
        setupResp.EnsureSuccessStatusCode();
        var setup = await setupResp.Content.ReadFromJsonAsync<JsonElement>();
        var secret = ExtractSecret(setup.GetProperty("otpauthUri").GetString()!);

        var enableResp = await authed.PostAsJsonAsync("/api/account/mfa/enable", new { code = Totp(secret) });
        Assert.Equal(HttpStatusCode.OK, enableResp.StatusCode);
        var enableJson = await enableResp.Content.ReadFromJsonAsync<JsonElement>();
        var recoveryCodes = enableJson.GetProperty("recoveryCodes").EnumerateArray()
            .Select(e => e.GetString()!).ToList();
        Assert.Equal(10, recoveryCodes.Count);

        // 3. Password alone is no longer enough.
        var missingCode = await PasswordGrantAsync(client);
        Assert.Equal(HttpStatusCode.BadRequest, missingCode.Status);
        Assert.Equal("mfa_required", missingCode.Json.GetProperty("error").GetString());

        // 4. A wrong code is rejected.
        var wrongCode = await PasswordGrantAsync(client, mfaCode: "000000");
        Assert.Equal(HttpStatusCode.BadRequest, wrongCode.Status);
        Assert.Equal("invalid_grant", wrongCode.Json.GetProperty("error").GetString());

        // 5. A live TOTP unlocks tokens.
        var withTotp = await PasswordGrantAsync(client, mfaCode: Totp(secret));
        Assert.True(withTotp.Status == HttpStatusCode.OK,
            $"Expected OK, got {withTotp.Status}: {withTotp.Json.GetRawText()}");
        Assert.True(withTotp.Json.TryGetProperty("access_token", out _));

        // 6. A recovery code also unlocks (and is single-use).
        var withRecovery = await PasswordGrantAsync(client, mfaCode: recoveryCodes[0]);
        Assert.Equal(HttpStatusCode.OK, withRecovery.Status);
        var replayRecovery = await PasswordGrantAsync(client, mfaCode: recoveryCodes[0]);
        Assert.Equal(HttpStatusCode.BadRequest, replayRecovery.Status);

        // 7. Disable restores plain password login (leave the account clean).
        var authed2 = WithBearer(client, withTotp.Json);
        var disableResp = await authed2.PostAsJsonAsync("/api/account/mfa/disable", new { code = Totp(secret) });
        Assert.Equal(HttpStatusCode.NoContent, disableResp.StatusCode);

        var plainAgain = await PasswordGrantAsync(client);
        Assert.Equal(HttpStatusCode.OK, plainAgain.Status);
    }

    [Fact]
    public async Task Sso_ticket_grant_issues_tokens_once_and_rejects_replay()
    {
        var client = _factory.CreateClient();

        string ticket;
        using (var scope = _factory.Services.CreateScope())
        {
            var tickets = scope.ServiceProvider.GetRequiredService<SsoTicketService>();
            ticket = await tickets.IssueAsync(AdminUserId, "test");
        }

        var first = await SsoGrantAsync(client, ticket);
        Assert.Equal(HttpStatusCode.OK, first.Status);
        Assert.True(first.Json.TryGetProperty("access_token", out _));

        // Single-use: replaying the same ticket must fail.
        var replay = await SsoGrantAsync(client, ticket);
        Assert.Equal(HttpStatusCode.BadRequest, replay.Status);
        Assert.Equal("invalid_grant", replay.Json.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Sso_providers_endpoint_is_anonymous_and_empty_when_disabled()
    {
        var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/auth/sso/providers");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, json.GetArrayLength());
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private sealed record TokenResult(HttpStatusCode Status, JsonElement Json);

    private static async Task<TokenResult> PasswordGrantAsync(HttpClient client, string? mfaCode = null)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "password",
            ["username"] = AdminUser,
            ["password"] = AdminPass,
            ["scope"] = "openid email profile roles offline_access",
        };
        if (mfaCode != null)
        {
            form["mfa_code"] = mfaCode;
        }
        var resp = await client.PostAsync("/connect/token", new FormUrlEncodedContent(form));
        return new TokenResult(resp.StatusCode, await resp.Content.ReadFromJsonAsync<JsonElement>());
    }

    private static async Task<TokenResult> SsoGrantAsync(HttpClient client, string ticket)
    {
        var form = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:werkpilot:sso",
            ["ticket"] = ticket,
            ["scope"] = "openid email profile roles offline_access",
        };
        var resp = await client.PostAsync("/connect/token", new FormUrlEncodedContent(form));
        return new TokenResult(resp.StatusCode, await resp.Content.ReadFromJsonAsync<JsonElement>());
    }

    private static HttpClient WithBearer(HttpClient client, JsonElement tokenJson)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", tokenJson.GetProperty("access_token").GetString());
        return client;
    }

    private static string ExtractSecret(string otpauthUri)
    {
        var query = new Uri(otpauthUri).Query.TrimStart('?');
        foreach (var pair in query.Split('&'))
        {
            var kv = pair.Split('=', 2);
            if (kv[0] == "secret") return kv[1];
        }
        throw new InvalidOperationException("otpauth URI has no secret.");
    }

    /// <summary>RFC 6238 TOTP (HMAC-SHA1, 30 s step, 6 digits) over a base32 key.</summary>
    private static string Totp(string base32Key)
    {
        var key = Base32Decode(base32Key);
        var counter = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }
        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
                     | (hash[offset + 1] << 16)
                     | (hash[offset + 2] << 8)
                     | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }

    private static byte[] Base32Decode(string input)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        input = input.TrimEnd('=').ToUpperInvariant();
        var bits = 0;
        var value = 0;
        var output = new List<byte>(input.Length * 5 / 8);
        foreach (var c in input)
        {
            value = (value << 5) | alphabet.IndexOf(c);
            bits += 5;
            if (bits >= 8)
            {
                output.Add((byte)((value >> (bits - 8)) & 0xff));
                bits -= 8;
            }
        }
        return output.ToArray();
    }
}
