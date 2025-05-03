using Fin.Domain.Global.Interfaces;
using Fin.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fin.Infrastructure.Database;

public class AuditedEntityInterceptor: SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditedEntityInterceptor(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = _dateTimeProvider.UtcNow();

        foreach (var entry in context.ChangeTracker.Entries<IAuditedEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.UpdatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Property(x => x.CreatedAt).IsModified = false;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}