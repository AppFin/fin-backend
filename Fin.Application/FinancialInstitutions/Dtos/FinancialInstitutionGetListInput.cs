using Fin.Domain.FinancialInstitutions.Enums;
using Fin.Domain.Global.Classes;

namespace Fin.Application.FinancialInstitutions.Dtos;

public class FinancialInstitutionGetListInput : PagedFilteredAndSortedInput
{
    public bool? Inactive { get; set; }
    public FinancialInstitutionType? Type { get; set; }
    
}