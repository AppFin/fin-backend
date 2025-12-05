using Fin.Domain.Global.Interfaces;
using Fin.Domain.People.Dtos;
using Fin.Domain.Titles.Entities;

namespace Fin.Domain.People.Entities;

public class Person: ILoggableAuditedTenantEntity
{
    public string Name { get; private set; }
    public bool Inactivated { get; private set; }
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }

    public virtual ICollection<Title> Titles { get; set; } = [];
    public virtual ICollection<TitlePerson> TitlePeople { get; set; } = [];

    public Person()
    {
    }
    
    public Person(PersonInput input)
    {
        Name = input.Name;
    }

    public void Update(PersonInput input)
    {
        Name = input.Name;
    }
    
    public void ToggleInactivated()
    {
        Inactivated = !Inactivated;
    }

    public object GetLog()
    {
        return new
        {
            Id,
            CreatedAt,
            CreatedBy,
            UpdatedAt,
            UpdatedBy,
            TenantId,
            Name,
            Inactivated
        };
    }
}