using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OmniFlow.Application.Interfaces;
using OmniFlow.Domain.Common.Exceptions;
using OmniFlow.Domain.Entities;
using OmniFlow.Domain.Enums;
using OmniFlow.Infrastructure.Persistence;
using OmniFlow.Infrastructure.Persistence.Seed;

namespace OmniFlow.Infrastructure.Identity;

public class TenantProvisioningService : ITenantProvisioningService
{
    private readonly OmniFlowDbContext _dbContext;
    private readonly ITenantService _tenantService;
    private readonly IIdentityAccountService _accounts;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        OmniFlowDbContext dbContext,
        ITenantService tenantService,
        IIdentityAccountService accounts,
        ILogger<TenantProvisioningService> logger)
    {
        _dbContext = dbContext;
        _tenantService = tenantService;
        _accounts = accounts;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TenantSummary>> ListAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _dbContext.Tenants
            .OrderBy(t => t.Name)
            .Select(t => new { t.Id, t.Name, t.Domain, t.IsActive, t.CreatedAt })
            .ToListAsync(cancellationToken);

        // Counts are cross-tenant by nature, so the global filter is bypassed
        // and grouping happens per tenant id.
        var userCounts = await _dbContext.Users.IgnoreQueryFilters()
            .Where(u => !u.IsDeleted)
            .GroupBy(u => u.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        var ticketCounts = await _dbContext.Tickets.IgnoreQueryFilters()
            .Where(t => !t.IsDeleted)
            .GroupBy(t => t.TenantId)
            .Select(g => new { TenantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.TenantId, x => x.Count, cancellationToken);

        return tenants.Select(t => new TenantSummary(
            t.Id, t.Name, t.Domain, t.IsActive, t.CreatedAt,
            userCounts.GetValueOrDefault(t.Id),
            ticketCounts.GetValueOrDefault(t.Id))).ToList();
    }

    public async Task<Guid> ProvisionAsync(ProvisionTenantRequest request, CancellationToken cancellationToken = default)
    {
        var domain = request.Domain.Trim().ToLowerInvariant();
        var adminEmail = request.AdminEmail.Trim().ToLowerInvariant();

        if (await _dbContext.Tenants.AnyAsync(t => t.Domain == domain, cancellationToken))
        {
            throw new ConflictException($"A tenant already exists for domain '{domain}'.");
        }

        // Login accounts are unique platform-wide, so the first admin's address
        // must not already belong to another tenant's user.
        var emailTaken = await _dbContext.Users.IgnoreQueryFilters()
            .AnyAsync(u => u.Email == adminEmail && !u.IsDeleted, cancellationToken);
        if (emailTaken)
        {
            throw new ConflictException($"'{adminEmail}' already belongs to an existing user.");
        }

        var tenantId = Guid.NewGuid();
        var previousTenant = _tenantService.TenantId;

        _dbContext.Tenants.Add(new Tenant
        {
            Id = tenantId,
            Name = request.Name.Trim(),
            Domain = domain,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });
        await _dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            // Everything below belongs to the NEW tenant: pin the ambient tenant
            // so the auditing interceptor stamps rows and the global query
            // filters (used by the baseline's "already seeded?" checks) resolve
            // against it rather than the caller's own tenant.
            _tenantService.SetTenantId(tenantId);

            await TenantBaseline.SeedAsync(
                _dbContext, tenantId, TenantBaselineIds.New(), "Head Office", cancellationToken);

            var departmentId = await _dbContext.Departments
                .Select(d => (Guid?)d.Id)
                .FirstOrDefaultAsync(cancellationToken);

            var adminId = Guid.NewGuid();
            _dbContext.Users.Add(new User
            {
                Id = adminId,
                TenantId = tenantId,
                Name = request.AdminName.Trim(),
                Email = adminEmail,
                Role = UserRole.Administrator,
                DepartmentId = departmentId
            });
            await _dbContext.SaveChangesAsync(cancellationToken);

            // Passwordless + invitation: the new admin sets their own password
            // through the emailed link.
            await _accounts.CreateAccountAsync(
                adminId, adminEmail, initialPassword: null,
                identityRole: "Administrator", sendInvite: true, cancellationToken);

            _logger.LogInformation("Provisioned tenant {TenantName} ({TenantId}) with admin {AdminEmail}.",
                request.Name, tenantId, adminEmail);

            return tenantId;
        }
        finally
        {
            _tenantService.SetTenantId(previousTenant);
        }
    }

    public async Task SetActiveAsync(Guid tenantId, bool isActive, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, cancellationToken)
                     ?? throw new NotFoundException(nameof(Tenant), tenantId);

        tenant.IsActive = isActive;
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Tenant {TenantId} {State}.", tenantId, isActive ? "activated" : "deactivated");
    }
}
