using Fin.Domain.Global.Interfaces;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Audits.Enums;
using Fin.Infrastructure.Audits.Interfaces;
using Fin.Infrastructure.DateTimes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MongoDB.Bson;

namespace Fin.Infrastructure.Audits;

public class AuditLogInterceptor(
    IAuditLogService auditService,
    IAmbientData ambientData,
    IDateTimeProvider dateTimeProvider
    ) : SaveChangesInterceptor
{
    private readonly List<AuditEntry> _temporaryAuditEntries = new();

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        _temporaryAuditEntries.Clear();
        var context = eventData.Context;
        if (context == null) return base.SavingChangesAsync(eventData, result, cancellationToken);

        context.ChangeTracker.DetectChanges();
        
        foreach (var entry in context.ChangeTracker.Entries<ILoggable>())
        {
            if (entry.State is EntityState.Detached or EntityState.Unchanged) continue;

            var auditEntry = new AuditEntry(entry, ambientData);
            _temporaryAuditEntries.Add(auditEntry);

            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }
                
                if (entry.State == EntityState.Modified && property.IsModified)
                {
                    auditEntry.PreviousValues[property.Metadata.Name] = ConvertToBsonValid(property.OriginalValue);
                }
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, 
        int result, 
        CancellationToken cancellationToken = default)
    {
        if (_temporaryAuditEntries == null || _temporaryAuditEntries.Count == 0)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var mongoLogs = new List<AuditLogDocument>();

        foreach (var entry in _temporaryAuditEntries)
        {
            foreach (var prop in entry.TemporaryProperties.Where(prop => prop.Metadata.IsPrimaryKey()))
            {
                entry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
            }

            var action = AuditLogAction.Updated;
            switch (entry.Entry.State)
            {
                case EntityState.Deleted:
                    action = AuditLogAction.Deleted;
                    break;
                case EntityState.Added:
                    action = AuditLogAction.Created;
                    break;
            }
            
            mongoLogs.Add(new AuditLogDocument
            {
                EntityName = entry.TableName,
                Action = action,
                UserId = entry.UserId,
                TenantId = entry.TenantId,
                DateTime = dateTimeProvider.UtcNow(),
                Snapshot = entry.Snapshot,
                PreviousValues = entry.PreviousValues.Any() ? entry.PreviousValues.ToBsonDocument() : null,
                KeyValues = entry.KeyValues 
            });
        }

        await auditService.LogAsync(mongoLogs);
        _temporaryAuditEntries.Clear();
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
    
    private static object ConvertToBsonValid(object value)
    {
        return value switch
        {
            null => null,
            Guid guid => guid.ToString(),
            _ => value
        };
    }
}