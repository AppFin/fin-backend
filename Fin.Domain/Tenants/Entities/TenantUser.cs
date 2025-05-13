namespace Fin.Domain.Tenants.Entities;

public class TenantUser
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
}