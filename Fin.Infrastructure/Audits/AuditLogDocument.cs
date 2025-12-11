using Fin.Infrastructure.Audits.Enums;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Fin.Infrastructure.Audits;

public class AuditLogDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string InternalId { get; set; }

    public string EntityName { get; set; }
    public string EntityId { get; set; }
    
    public AuditLogAction Action { get; set; }
    public DateTime DateTime { get; set; }
    
    public object NewValue { get; set; } 
    public object OldValue { get; set; } 
    
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
}