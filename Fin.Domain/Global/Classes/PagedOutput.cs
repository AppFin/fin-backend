namespace Fin.Domain.Global.Classes;

public class PagedOutput<T>
{
    public List<T> Items { get; set; }
    public int TotalCount { get; set; }

    public PagedOutput()
    {
    }

    public PagedOutput(int totalCount, List<T> items)
    {
        Items = items;
        TotalCount = totalCount;
    }
}