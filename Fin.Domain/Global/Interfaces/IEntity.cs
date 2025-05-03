using System.ComponentModel.DataAnnotations;

namespace Fin.Domain.Global.Interfaces;

public interface IEntity
{
    [Key]
    public Guid Id { get; set; }
}