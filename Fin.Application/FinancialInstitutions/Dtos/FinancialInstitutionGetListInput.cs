using Fin.Domain.FinancialInstitutions.Enums;
using Fin.Domain.Global.Classes;

public class FinancialInstitutionGetListInput : PagedFilteredAndSortedInput
{
    public bool? Inactive { get; set; }
    public FinancialInstitutionType? Type { get; set; }
    
}