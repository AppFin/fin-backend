using Fin.Domain.Global.Classes;

namespace Fin.Application.CreditCards.Dtos;

public class CreditCardGetListInput: PagedFilteredAndSortedInput
{
    public bool? Inactivated { get; set; }
    public List<Guid> DebitWalletIds { get; set; } = [];
    public List<Guid> FinancialInstitutionIds { get; set; } = [];
    public List<Guid> CardBrandIds { get; set; } = [];
}