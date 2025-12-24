using Fin.Domain.People.Entities;

namespace Fin.Domain.People.Dtos;

public class CreditChargePersonOutput(CreditChargePerson creditChargePerson)
{
    public Guid PersonId { get; set; } = creditChargePerson.PersonId;
    public decimal Percentage {get; set;} = creditChargePerson.Percentage;
}