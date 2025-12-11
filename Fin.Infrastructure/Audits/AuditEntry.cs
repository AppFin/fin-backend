using Fin.Domain.Global.Interfaces;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Audits.Enums;
using Fin.Infrastructure.Audits.Interfaces;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Fin.Infrastructure.Audits;

public class AuditEntry(IAmbientData ambientData)
{
    public string EntityName { get; set; }
    public string EntityId { get; set; }
    public object NewValue { get; set; }
    public object OldValue { get; set; }

    public AuditLogAction Action { get; set; }
    public Guid UserId { get; set; } = ambientData.UserId.GetValueOrDefault();
    public Guid TenantId { get; set; } = ambientData.TenantId.GetValueOrDefault();
}