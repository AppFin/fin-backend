using System.ComponentModel.DataAnnotations;

namespace Fin.Domain.Global.Interfaces;

public interface ITenantEntity: IEntity
{
    public Guid TenantId { get; set; }
}