using Fin.Infrastructure.Audits.Interfaces;
using Fin.Infrastructure.DateTimes;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Fin.Infrastructure.Audits;

public class MongoAuditLogService(
    IMongoDatabase database,
    IDateTimeProvider dateTimeProvider,
    ILogger<MongoAuditLogService> logger
    ) : IAuditLogService
{
    private readonly IMongoCollection<AuditLogDocument> _collection = database.GetCollection<AuditLogDocument>("audit_logs");

    public async Task LogAsync(List<AuditEntry> logs)
    {
        try
        {
            var entitiesLog = logs.Select(log => new AuditLogDocument
            {
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                NewValue = log.NewValue,
                OldValue = log.OldValue,
                Action = log.Action,
                DateTime = dateTimeProvider.UtcNow(),
                UserId = log.UserId,
                TenantId = log.TenantId
            });

            await _collection.InsertManyAsync(entitiesLog);
        }
        catch (Exception ex)
        {
            logger.LogError("Error on saving log: {ExMessage}", ex.Message);
        }
    }

    public void Log(List<AuditEntry> logs)
    {
        try
        {

            var entitiesLog = logs.Select(log => new AuditLogDocument
            {
                EntityName = log.EntityName,
                EntityId = log.EntityId,
                NewValue = log.NewValue,
                OldValue = log.OldValue,
                Action = log.Action,
                DateTime = dateTimeProvider.UtcNow(),
                UserId = log.UserId,
                TenantId = log.TenantId
            });

            _collection.InsertMany(entitiesLog);
        }
        catch (Exception ex)
        {
            logger.LogError("Error on saving log: {ExMessage}", ex.Message);
        }
    }
}