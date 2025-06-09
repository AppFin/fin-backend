using Fin.Domain.Global.Interfaces;
using Fin.Infrastructure.AmbientDatas;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fin.Infrastructure.Database.Interceptors;

public class TenantEntityInterceptor: SaveChangesInterceptor
{
    private readonly IAmbientData _ambientData;

    public TenantEntityInterceptor(IAmbientData ambientData)
    {
        _ambientData = ambientData;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context == null || !_ambientData.IsLogged) return base.SavingChangesAsync(eventData, result, cancellationToken);
        
        var tenantId = _ambientData.TenantId.GetValueOrDefault();
        var hasTenantId = tenantId != Guid.Empty;
        
        if (!hasTenantId) return base.SavingChangesAsync(eventData, result, cancellationToken);
        
        foreach (var entry in context.ChangeTracker.Entries<ITenantEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.TenantId = _ambientData.TenantId.GetValueOrDefault();
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(x => x.TenantId).IsModified = false;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}