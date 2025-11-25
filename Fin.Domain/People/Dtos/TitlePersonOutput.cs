using Fin.Domain.People.Entities;

namespace Fin.Domain.People.Dtos;

public class TitlePersonOutput(TitlePerson titlePerson)
{
    public Guid PersonId { get; set; } = titlePerson.PersonId;
    public decimal Percentage {get; set;} = titlePerson.Percentage;
}