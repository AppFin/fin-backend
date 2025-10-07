namespace Fin.Domain.Global.Interfaces;

public interface IAuditedEntity: IEntity
{
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}