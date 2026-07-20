using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Persistence;
using OmniFlow.Infrastructure.Persistence.Seed;

namespace OmniFlow.Infrastructure.Identity;

/// <summary>
/// Resolves an externally-authenticated identity (LDAP, corporate SSO) to a
/// local account. With auto-provisioning enabled, first login creates the
/// Identity account plus its mirrored domain <see cref="User"/> row (same id —
/// the rest of the platform keys on that id for requester/approver semantics).
/// </summary>
public class ExternalUserProvisioner
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly OmniFlowDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly ILogger<ExternalUserProvisioner> _logger;

    public ExternalUserProvisioner(
        UserManager<ApplicationUser> userManager,
        OmniFlowDbContext dbContext,
        ITenantService tenantService,
        ILogger<ExternalUserProvisioner> logger)
    {
        _userManager = userManager;
        _dbContext = dbContext;
        _tenantService = tenantService;
        _logger = logger;
    }

    public async Task<ApplicationUser?> FindOrProvisionAsync(
        string email,
        string? displayName,
        string source,
        bool autoProvision,
        string defaultRole,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userManager.FindByEmailAsync(email)
                       ?? await _userManager.FindByNameAsync(email);
        if (existing != null)
        {
            return existing;
        }

        if (!autoProvision)
        {
            _logger.LogWarning("{Source} login for {Email} rejected: no local account and auto-provisioning is disabled.", source, email);
            return null;
        }

        // External logins land in the default tenant until per-tenant IdP
        // routing exists; the tenant service must be scoped before the domain
        // row is saved so the auditing interceptor stamps the right tenant.
        var tenantId = SeedConstants.DefaultTenantId;
        _tenantService.SetTenantId(tenantId);

        var id = Guid.NewGuid();
        var account = new ApplicationUser
        {
            Id = id,
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            TenantId = tenantId
        };

        var created = await _userManager.CreateAsync(account); // passwordless — external directory owns the credential
        if (!created.Succeeded)
        {
            _logger.LogError("Auto-provisioning {Email} from {Source} failed: {Errors}",
                email, source, string.Join("; ", created.Errors.Select(e => e.Description)));
            return null;
        }

        await _userManager.AddToRoleAsync(account, defaultRole);

        _dbContext.Set<User>().Add(new User
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(displayName) ? email : displayName,
            Email = email,
            Role = UserRole.Requester,
            DepartmentId = null
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Auto-provisioned {Email} from {Source} with role {Role}.", email, source, defaultRole);
        return account;
    }
}
