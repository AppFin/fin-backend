using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fin.Infrastructure.Database.ValueConverters;

public class ListToStringConverter<TEnum>() : ValueConverter<List<TEnum>, string>(
    v => string.Join(",", v.Select(e => e.ToString())),
    v => string.IsNullOrWhiteSpace(v)
        ? new List<TEnum>()
        : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(Enum.Parse<TEnum>)
            .ToList())
    where TEnum : struct, Enum;

public class ListToStringConverter() : ValueConverter<List<string>, string>(
    v => string.Join(",", v.Select(e => e.ToString())),
    v => string.IsNullOrWhiteSpace(v)
        ? new List<string>()
        : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .ToList());