using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.Global.Classes;

public class PagedFilteredAndSortedInput: IPagedInput, IFilteredAndSortedInput
{
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
    public FilteredProperty Filter { get; set; }
    public List<SortedProperty> Sorts { get; set; } = new();
}