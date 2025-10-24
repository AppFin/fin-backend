using Fin.Domain.Titles.Entities;

namespace Fin.Domain.Titles.Extensions;

public static class TitleExtensions
{
    public static IEnumerable<Title> ApplyDefaultTitleOrder(this IEnumerable<Title> titles)
    {
        return titles.OrderBy(m => m.Date).ThenBy(m => m.Id);
    }
    
    public static IQueryable<Title> ApplyDefaultTitleOrder(this IQueryable<Title> titles)
    {
        return titles.OrderBy(m => m.Date).ThenBy(m => m.Id);
    }
}