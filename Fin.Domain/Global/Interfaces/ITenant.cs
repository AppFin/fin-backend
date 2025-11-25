namespace Fin.Domain.Global.Interfaces;

public interface ITenant
{
    public Guid TenantId { get; set; }
}