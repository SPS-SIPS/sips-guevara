using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SPSS.Infrastructure.Persistence.Interceptors;

public class VersionedEntityInterceptor() : SaveChangesInterceptor
{
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

        foreach (var entry in context.ChangeTracker.Entries<IVersionedEntity>())
        {
            if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
            {
                long originalVersion = GetOrigin(entry.OriginalValues[nameof(IVersionedEntity.Version)]);
                long currentVersion = entry.Entity.Version;

                if (originalVersion != currentVersion)
                {
                    throw new DbUpdateConcurrencyException($"The entity {entry.Entity.GetType().Name} has been modified by another user.");
                }
            }

            entry.Entity.Version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    private static long GetOrigin(object? value)
    {
        if (value is null)
        {
            return 0;
        }
        else
        {
            return (long)value;
        }
    }
}