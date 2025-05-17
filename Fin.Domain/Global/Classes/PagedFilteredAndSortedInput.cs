using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.Global.Classes;

public class PagedFilteredAndSortedInput: IPagedInput, IFilteredAndSortedInput
{
    public int SkipCount { get; set; } = 0;
    public int MaxResultCount { get; set; } = 25;
    public FilteredProperty Filter { get; set; }
    public List<SortedProperty> Sorts { get; set; } = new();
}