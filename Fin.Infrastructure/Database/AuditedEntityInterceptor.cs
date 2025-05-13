using Fin.Domain.Global.Interfaces;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices;
using Fin.Infrastructure.DateTimes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fin.Infrastructure.Database;

public class AuditedEntityInterceptor: SaveChangesInterceptor
{
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IAmbientData _ambientData;

    public AuditedEntityInterceptor(IDateTimeProvider dateTimeProvider, IAmbientData ambientData)
    {
        _dateTimeProvider = dateTimeProvider;
        _ambientData = ambientData;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = _dateTimeProvider.UtcNow();
        var useId = _ambientData.UserId;
        

        foreach (var entry in context.ChangeTracker.Entries<IAuditedEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
                entry.Entity.CreatedBy = useId ?? entry.Entity.CreatedBy;
                
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = useId ?? entry.Entity.UpdatedBy;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Entity.UpdatedBy = useId ?? entry.Entity.UpdatedBy;
                
                entry.Property(x => x.CreatedAt).IsModified = false;
                entry.Property(x => x.CreatedBy).IsModified = false;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}