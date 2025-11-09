using Fin.Domain.People.Entities;

namespace Fin.Domain.People.Dtos;

public class PersonOutput(Person person)
{
    public Guid Id { get; private set; } = person.Id;
    public string Name { get; private set; } = person.Name;
    public bool Inactivated { get; private set; } = person.Inactivated;
}