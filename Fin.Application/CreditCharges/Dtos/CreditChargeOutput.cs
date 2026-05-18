using Fin.Domain.CreditCharges.Entities;
using Fin.Domain.People.Dtos;

namespace Fin.Application.CreditCharges.Dtos;

public class CreditChargeOutput(CreditCharge charge)
{
    public Guid Id { get; set; } = charge.Id;
    public string Description { get; set; } = charge.Description;
    public decimal Value { get; set; } = charge.Value;
    public int NumberOfInstallments { get; set; } = charge.NumberOfInstallments;
    public DateTime Date { get; set; } = charge.Date;
    public Guid CreditCardId { get; set; } = charge.CreditCardId;
    public List<Guid> CreditChargeCategoriesIds { get; set; } = charge.CreditChargeCategories
        .Select(x => x.TitleCategoryId).ToList();
    public List<CreditChargePersonOutput> CreditChargePeople { get; set; } = charge.CreditChargePeople
        .Select(x => new CreditChargePersonOutput(x)).ToList();

    public CreditChargeOutput(): this(new CreditCharge())
    {
    }
}


