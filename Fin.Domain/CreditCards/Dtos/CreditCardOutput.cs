using Fin.Domain.CreditCards.Entities;

namespace Fin.Domain.CreditCards.Dtos;

public class CreditCardOutput(CreditCard card)
{
    public string Name { get; set; } = card.Name;
    public string Color { get; set; } = card.Color;
    public string Icon { get; set; } = card.Icon;
    public decimal Limit { get; set; } = card.Limit;
    public int DueDay { get; set; } = card.DueDay;
    public int ClosingDay { get; set; } = card.ClosingDay;
    public Guid DebitWalletId { get; set; } = card.DebitWalletId;
    public Guid CardBrandId { get; set; } = card.CardBrandId;
    public Guid FinancialInstitutionId { get; set; } = card.FinancialInstitutionId;

    public CreditCardOutput(): this(new CreditCard())
    {
    }
}