using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OmniFlow.Domain.Common;

namespace OmniFlow.Infrastructure.Persistence.Interceptors;

public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly OmniFlow.Application.Interfaces.ITenantService _tenantService;

    public AuditableEntityInterceptor(OmniFlow.Application.Interfaces.ITenantService tenantService)
    {
        _tenantService = tenantService;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<AuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                // In a real scenario, retrieve UserId from HttpContext or Identity Service
                entry.Entity.CreatedBy = Guid.Empty; 
                entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                entry.Entity.TenantId = _tenantService.TenantId;
            }

            if (entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.ModifiedBy = Guid.Empty;
                entry.Entity.ModifiedAt = DateTimeOffset.UtcNow;
            }
        }
    }
}

public static class ChangeTrackerExtensions
{
    public static bool HasChangedOwnedEntities(this Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry entry) =>
        entry.References.Any(r => 
            r.TargetEntry != null && 
            r.TargetEntry.Metadata.IsOwned() && 
            (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}
