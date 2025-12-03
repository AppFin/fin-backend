namespace Fin.Infrastructure.Audits.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(List<AuditEntry> logs);
    void Log(List<AuditEntry> logs);
}