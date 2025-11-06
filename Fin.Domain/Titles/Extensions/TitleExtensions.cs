using Fin.Domain.Titles.Entities;

namespace Fin.Domain.Titles.Extensions;

public static class TitleExtensions
{
    public static IEnumerable<Title> ApplyDefaultTitleOrder(this IEnumerable<Title> titles)
    {
        return titles.OrderByDescending(m => m.Date).ThenByDescending(m => m.Id);
    }
    
    public static IQueryable<Title> ApplyDefaultTitleOrder(this IQueryable<Title> titles)
    {
        return titles.OrderByDescending(m => m.Date).ThenByDescending(m => m.Id);
    }
}