namespace Fin.Domain.Global.Interfaces;

public interface ILoggableAuditedTenantEntity: ILoggable, IAuditedEntity, ITenantEntity
{
    
}