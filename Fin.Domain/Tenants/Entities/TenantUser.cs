using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.Tenants.Entities;

public class TenantUser: ILoggable
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    
    public object GetLog()
    {
        return new
        {
            TenantId,
            UserId
        };
    }
}