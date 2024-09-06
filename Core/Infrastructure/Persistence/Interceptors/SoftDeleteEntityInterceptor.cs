using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SPSS.Infrastructure.Persistence.Interceptors;

public class SoftDeleteEntityInterceptor(IIdentityService identity, TimeProvider time) : SaveChangesInterceptor
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

        foreach (var entry in context.ChangeTracker.Entries<ISoftDelete>())
        {

            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.DeletedBy = _identity.Username;
                entry.Entity.DeletedOn = _DateTimeOffset.GetUtcNow();
            }

            else if (entry.State == EntityState.Modified || entry.State == EntityState.Unchanged)
            {
                entry.Entity.DeletedBy = null;
                entry.Entity.DeletedOn = null;
            }
        }
    }
}