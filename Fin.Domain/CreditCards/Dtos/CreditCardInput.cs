namespace Fin.Domain.CreditCards.Dtos;

public class CreditCardInput
{
    public string Name { get; set; }
    public string Color { get; set; }
    public string Icon { get; set; }
    public decimal Limit { get; set; }
    public int DueDay { get; set; }
    public int ClosingDay { get; set; }
    public Guid DebitWalletId { get; set; }
    public Guid CardBrandId { get; set; }
    public Guid FinancialInstitutionId { get; set; }
}