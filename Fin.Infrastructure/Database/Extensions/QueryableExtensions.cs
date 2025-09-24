using System.Linq.Expressions;
using System.Reflection;
using Fin.Domain.Global.Classes;
using Fin.Domain.Global.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Fin.Infrastructure.Database.Extensions;

public static class QueryableExtensions
{
    public static IEnumerable<T> WhereIf<T>(this IEnumerable<T> source, bool condition, Func<T, bool> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    public static IQueryable<T> WhereIf<T>(this IQueryable<T> source, bool condition,
        Expression<Func<T, bool>> predicate)
    {
        return condition ? source.Where(predicate) : source;
    }

    public static IQueryable<T> ApplySorter<T>(this IQueryable<T> query, ISortedInput input)
    {
        if (input?.Sorts == null || input.Sorts.Count == 0)
            return query;

        IOrderedQueryable<T>? orderedQuery = null;

        foreach (var (sort, index) in input.Sorts.Select((s, i) => (s, i)))
        {
            var property = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => string.Equals(p.Name, sort.Property, StringComparison.OrdinalIgnoreCase));

            if (property == null)
                throw new ArgumentException($"Property '{sort.Property}' not found in type '{typeof(T).Name}'.");

            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.Property(parameter, property);
            var lambda = Expression.Lambda(propertyAccess, parameter);

            string methodName = GetMethodName(index, sort.Desc);

            var result = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName
                            && m.GetParameters().Length == 2)
                .MakeGenericMethod(typeof(T), property.PropertyType)
                .Invoke(null, new object[] { index == 0 ? query : orderedQuery!, lambda });

            orderedQuery = (IOrderedQueryable<T>)result!;
        }

        return orderedQuery ?? query;
    }

    public static IQueryable<T> ApplyFilter<T>(this IQueryable<T> query, IFilteredInput input)
    {
        if (input?.Filter == null ||
            string.IsNullOrWhiteSpace(input.Filter.Property) ||
            string.IsNullOrWhiteSpace(input.Filter.Filter))
            return query;

        var property = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                string.Equals(p.Name, input.Filter.Property, StringComparison.OrdinalIgnoreCase) &&
                p.PropertyType == typeof(string));

        if (property == null)
            throw new ArgumentException(
                $"Property '{input.Filter.Property}' not found in type '{typeof(T).Name}' or is not string.");

        var parameter = Expression.Parameter(typeof(T), "x");
        var propertyAccess = Expression.Property(parameter, property);

        var efFunctions = Expression.Property(null, typeof(EF).GetProperty(nameof(EF.Functions))!);

        var context = query.GetType().GetProperty("Provider")?.GetValue(query);

        var isPostgreSql = context?.ToString()?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true;

        Expression likeCall;

        if (isPostgreSql)
        {
            var iLikeMethod = typeof(NpgsqlDbFunctionsExtensions).GetMethod(nameof(NpgsqlDbFunctionsExtensions.ILike),
                new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

            var pattern = Expression.Constant($"%{input.Filter.Filter}%");
            likeCall = Expression.Call(null, iLikeMethod, efFunctions, propertyAccess, pattern);
        }
        else
        {
            var likeMethod = typeof(DbFunctionsExtensions).GetMethod(nameof(DbFunctionsExtensions.Like),
                new[] { typeof(DbFunctions), typeof(string), typeof(string) })!;

            var toLowerMethod = typeof(string).GetMethod(nameof(string.ToLower), Type.EmptyTypes)!;
            var propertyToLower = Expression.Call(propertyAccess, toLowerMethod);

            var pattern = Expression.Constant($"%{input.Filter.Filter.ToLower()}%");
            likeCall = Expression.Call(null, likeMethod, efFunctions, propertyToLower, pattern);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(likeCall, parameter);

        return query.Where(lambda);
    }


    public static IQueryable<T> ApplyFilterAndSorter<T>(this IQueryable<T> query, IFilteredAndSortedInput input)
    {
        return query.ApplyFilter(input).ApplySorter(input);
    }

    public static async Task<PagedOutput<T>> ToPagedResult<T>(this IQueryable<T> query, IPagedInput input,
        CancellationToken cancellationToken = default)
    {
        return new PagedOutput<T>
        {
            TotalCount = await query.CountAsync(cancellationToken),
            Items = await query.Skip(input.SkipCount).Take(input.MaxResultCount).ToListAsync(cancellationToken)
        };
    }

    private static string GetMethodName(int index, bool desc)
    {
        if (index == 0)
            return desc ? "OrderByDescending" : "OrderBy";
        return desc ? "ThenByDescending" : "ThenBy";
    }
}