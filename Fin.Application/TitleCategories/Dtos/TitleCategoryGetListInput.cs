using Fin.Domain.Global.Classes;
using Fin.Domain.TitleCategories.Enums;

namespace Fin.Application.TitleCategories.Dtos;

public class TitleCategoryGetListInput: PagedFilteredAndSortedInput
{
    public bool? Inactivated { get; set; }
    public TitleCategoryType? Type { get; set; }
}