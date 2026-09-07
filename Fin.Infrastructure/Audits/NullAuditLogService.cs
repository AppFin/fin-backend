using Fin.Infrastructure.Audits.Interfaces;

namespace Fin.Infrastructure.Audits;

/// Used instead of MongoAuditLogService in portfolio demo mode, so the app
/// never opens a MongoDB connection at all (no container, no wasted resources
/// retrying a connection). Audit trail is simply not recorded in this mode.
public class NullAuditLogService : IAuditLogService
{
    public Task LogAsync(List<AuditEntry> logs) => Task.CompletedTask;

    public void Log(List<AuditEntry> logs)
    {
    }
}
