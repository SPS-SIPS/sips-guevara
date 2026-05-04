using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SPSS.Infrastructure.Persistence.Interceptors;

public class AuditableEntityInterceptor(IIdentityService identity, TimeProvider time) : SaveChangesInterceptor
{
    private readonly IIdentityService _identity = identity;
    private readonly TimeProvider _DateTimeOffset = time;

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

    public void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedBy = _identity.Username;
                entry.Entity.CreatedOn = _DateTimeOffset.GetUtcNow();
            }

            if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.HasChangedOwnedEntities())
            {
                entry.Entity.LastModifiedBy = _identity.Username;
                entry.Entity.LastModifiedOn = _DateTimeOffset.GetUtcNow();
            }
        }
    }
}

public static class Extensions
{
    public static bool HasChangedOwnedEntities(this EntityEntry entry) =>
    entry.References.Any(r =>
       r.TargetEntry != null &&
       r.TargetEntry.Metadata.IsOwned() &&
       (r.TargetEntry.State == EntityState.Added || r.TargetEntry.State == EntityState.Modified));
}