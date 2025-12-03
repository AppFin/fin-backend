using Fin.Domain.Global.Interfaces;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Audits.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Fin.Infrastructure.Audits;

public class AuditEntry(EntityEntry<ILoggable> entry, IAmbientData ambientData)
{
    public EntityEntry<ILoggable> Entry { get; } = entry;
    public Guid UserId { get; } = ambientData.UserId.GetValueOrDefault(); 
    public Guid TenantId { get; } = ambientData.TenantId.GetValueOrDefault(); 
    public string TableName { get; } = entry.Entity.GetType().Name;

    public Dictionary<string, object> KeyValues { get; } = new();
    public object Snapshot { get; } = entry.Entity.GetLogSnapshot();
    public Dictionary<string, object> PreviousValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();
}