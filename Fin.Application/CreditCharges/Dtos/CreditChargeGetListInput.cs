using Fin.Domain.Global.Classes;
using Fin.Domain.Global.Enums;

namespace Fin.Application.CreditCharges.Dtos;

public class CreditChargeGetListInput: PagedFilteredAndSortedInput
{
    public List<Guid> CategoryIds { get; set; } = [];
    public MultiplyFilterOperator CategoryOperator { get; set; }
    public List<Guid> PersonIds { get; set; } = [];
    public MultiplyFilterOperator PersonOperator { get; set; }
    public List<Guid> CreditCardIds { get; set; } = [];
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
}

