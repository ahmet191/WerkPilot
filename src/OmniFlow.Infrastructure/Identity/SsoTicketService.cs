using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using OmniFlow.Infrastructure.Persistence;

namespace OmniFlow.Infrastructure.Identity;

/// <summary>
/// One-time exchange ticket bridging the browser SSO callback and the SPA's
/// token request. Only a SHA-256 hash is stored; tickets are single-use and
/// expire after <see cref="Lifetime"/>.
/// </summary>
public class SsoTicket
{
    public Guid Id { get; set; }
    public string CodeHash { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? ConsumedAt { get; set; }
}

public class SsoTicketService
{
    public static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    private readonly OmniFlowDbContext _dbContext;

    public SsoTicketService(OmniFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>Mints a ticket for the user and returns the raw code (never stored).</summary>
    public async Task<string> IssueAsync(Guid userId, string provider, CancellationToken cancellationToken = default)
    {
        var code = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        _dbContext.SsoTickets.Add(new SsoTicket
        {
            Id = Guid.NewGuid(),
            CodeHash = Hash(code),
            UserId = userId,
            Provider = provider,
            ExpiresAt = DateTimeOffset.UtcNow.Add(Lifetime)
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
        return code;
    }

    /// <summary>
    /// Atomically consumes a ticket. Returns the user id, or null when the
    /// ticket is unknown, expired, or already used (replay).
    /// </summary>
    public async Task<Guid?> RedeemAsync(string code, CancellationToken cancellationToken = default)
    {
        var hash = Hash(code);
        var now = DateTimeOffset.UtcNow;

        // Conditional UPDATE makes single-use race-safe across nodes.
        var consumed = await _dbContext.SsoTickets
            .Where(t => t.CodeHash == hash && t.ConsumedAt == null && t.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.ConsumedAt, now), cancellationToken);

        if (consumed == 0)
        {
            return null;
        }

        return await _dbContext.SsoTickets
            .Where(t => t.CodeHash == hash)
            .Select(t => (Guid?)t.UserId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string Hash(string code)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
}
