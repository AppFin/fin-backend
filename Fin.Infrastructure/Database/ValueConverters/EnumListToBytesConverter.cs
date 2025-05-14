using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fin.Infrastructure.Database.ValueConverters;

public class EnumListToStringConverter<TEnum>() : ValueConverter<List<TEnum>, string>(
    v => string.Join(",", v.Select(e => e.ToString())),
    v => string.IsNullOrWhiteSpace(v)
        ? new List<TEnum>()
        : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(Enum.Parse<TEnum>)
            .ToList())
    where TEnum : struct, Enum;