using System.Text.Json;
using Fin.Domain.Global.Interfaces;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Audits.Enums;
using Fin.Infrastructure.Audits.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Fin.Infrastructure.Audits;

public class AuditLogInterceptor(
    IAuditLogService auditService,
    IAmbientData ambientData
) : SaveChangesInterceptor
{
    private List<AuditEntry> _pendingLogs;

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CaptureChanges(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CaptureChanges(eventData.Context);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        SaveLogs();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await SaveLogsAsync();
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private void CaptureChanges(DbContext context)
    {
        _pendingLogs = new List<AuditEntry>();

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is ILoggable &&
                        (e.State == EntityState.Added ||
                         e.State == EntityState.Modified ||
                         e.State == EntityState.Deleted))
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.Entity is not ILoggable loggable) continue;

            var entityType = entry.Entity.GetType();
            var entityName = entityType.Name;

            var logEntry = new AuditEntry(ambientData)
            {
                EntityName = entityName,
                EntityId = GetEntityId(entry),
            };

            switch (entry.State)
            {
                case EntityState.Added:
                    logEntry.Action = AuditLogAction.Created;
                    logEntry.NewValue = loggable.GetLog();
                    logEntry.OldValue = null;
                    break;

                case EntityState.Modified:
                    logEntry.Action = AuditLogAction.Updated;
                    logEntry.NewValue = loggable.GetLog();
                    logEntry.OldValue = GetOriginalValues(entry);
                    break;

                case EntityState.Deleted:
                    logEntry.Action = AuditLogAction.Deleted;
                    logEntry.NewValue = null;
                    logEntry.OldValue = loggable.GetLog();
                    break;
            }

            _pendingLogs.Add(logEntry);
        }
    }

    private string GetEntityId(EntityEntry entry)
    {
        var keyValues = entry.Properties
            .Where(p => p.Metadata.IsKey())
            .Select(p => new { p.Metadata.Name, Value = p.CurrentValue })
            .ToList();

        if (keyValues.Count == 1)
        {
            return keyValues[0].Value?.ToString() ?? string.Empty;
        }

        var compositeKey = keyValues.ToDictionary(
            k => k.Name,
            k => k.Value
        );
        return JsonSerializer.Serialize(compositeKey);
    }

    private object GetOriginalValues(EntityEntry entry)
    {
        var loggable = entry.Entity as ILoggable;
        if (loggable == null) return null;

        var originalEntity = Activator.CreateInstance(entry.Entity.GetType());

        foreach (var property in entry.Properties)
        {
            var propInfo = entry.Entity.GetType().GetProperty(property.Metadata.Name);
            if (propInfo != null && propInfo.CanWrite)
            {
                propInfo.SetValue(originalEntity, property.OriginalValue);
            }
        }

        if (originalEntity is ILoggable loggableOriginal)
        {
            return loggableOriginal.GetLog();
        }

        return null;
    }

    private async Task SaveLogsAsync()
    {
        if (_pendingLogs.Count == 0) return;
        await auditService.LogAsync(_pendingLogs);
        _pendingLogs.Clear();
    }
    
    private void SaveLogs()
    {
        if (_pendingLogs.Count == 0) return;
        auditService.Log(_pendingLogs);
        _pendingLogs.Clear();
    }
}