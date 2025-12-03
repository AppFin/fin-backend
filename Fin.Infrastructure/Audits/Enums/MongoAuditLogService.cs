using Fin.Infrastructure.Audits.Interfaces;
using MongoDB.Driver;

namespace Fin.Infrastructure.Audits.Enums;

public class MongoAuditLogService(IMongoDatabase database) : IAuditLogService
{
    private readonly IMongoCollection<AuditLogDocument> _collection = database.GetCollection<AuditLogDocument>("audit_logs");

    public async Task LogAsync(List<AuditLogDocument> logs)
    {
        if (logs != null && logs.Any())
        {
            await _collection.InsertManyAsync(logs);
        }
    }
}