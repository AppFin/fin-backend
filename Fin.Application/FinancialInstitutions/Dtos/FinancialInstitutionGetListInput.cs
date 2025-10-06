using Fin.Domain.Global.Classes;

public class FinancialInstitutionGetListInput : PagedFilteredAndSortedInput
{
    public bool? Inactive { get; set; }
}