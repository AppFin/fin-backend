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
    [BsonIgnore]
    public Dictionary<string, object> KeyValues { get; set; } = new();
    public AuditLogAction Action { get; set; }
    public DateTime DateTime { get; set; }
    
    public Guid UserId { get; set; }
    public Guid TenantId { get; set; }
    
    public object Snapshot { get; set; } 
    public BsonDocument PreviousValues { get; set; }
    
    public string EntityIdString 
    { 
        get 
        {
            if (KeyValues == null || !KeyValues.Any()) return null;
            return KeyValues.Count == 1 ? KeyValues.Values.First().ToString() : string.Join("|", KeyValues.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }
    }
}