namespace Fin.Infrastructure.Audits.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(List<AuditLogDocument> logs);
}