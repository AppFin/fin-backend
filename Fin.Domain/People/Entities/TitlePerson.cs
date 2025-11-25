using Fin.Domain.Global.Interfaces;
using Fin.Domain.People.Dtos;
using Fin.Domain.Titles.Entities;

namespace Fin.Domain.People.Entities;

public class TitlePerson: ITenant, IAudited
{
    public Guid PersonId { get; private set; }
    public virtual Person Person { get; set; }
    
    public Guid TitleId { get; private  set; }
    public virtual Title Title { get; set; }
    
    public decimal Percentage {get; private  set;}

    public TitlePerson()
    {
    }
    
    public TitlePerson(Guid titleId, TitlePersonInput titlePerson)
    {
        TitleId = titleId;
        PersonId = titlePerson.PersonId;
        Percentage = titlePerson.Percentage;
    }

    public void Update(decimal percentage)
    {
        Percentage = percentage;
    }
    
    public Guid TenantId { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}