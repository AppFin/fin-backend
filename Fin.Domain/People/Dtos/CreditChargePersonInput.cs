namespace Fin.Domain.People.Dtos;

public class CreditChargePersonInput
{
    public Guid PersonId { get; set; }
    public decimal Percentage { get; set; }
}