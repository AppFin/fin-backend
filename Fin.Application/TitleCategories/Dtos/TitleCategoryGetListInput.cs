using Fin.Domain.Global.Classes;

namespace Fin.Application.TitleCategories.Dtos;

public class TitleCategoryGetListInput: PagedFilteredAndSortedInput
{
    public bool? Inactivated { get; set; }
}