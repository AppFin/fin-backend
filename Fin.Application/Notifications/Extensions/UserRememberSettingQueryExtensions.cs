using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Notifications.Extensions;

public static class UserRememberSettingQueryExtensions
{
    public static async Task<List<UserRememberUseSetting>> GetByWeekDayAndWaysCountAsync(
        this IRepository<UserRememberUseSetting> repository,
        DayOfWeek dayOfWeek,
        int minWaysCount = 0
        )
    {
        var dayOfWeekString = dayOfWeek.ToString();
        var databaseProvider = repository.Context.Database.ProviderName;

        var sql = databaseProvider switch
        {
            "Npgsql.EntityFrameworkCore.PostgreSQL" => @"
                SELECT * FROM ""UserRememberUseSettings""
                WHERE POSITION({0} IN ""WeekDays"") > 0 
                AND (CASE 
                    WHEN ""Ways"" IS NULL OR ""Ways"" = '' THEN 0 
                    ELSE array_length(string_to_array(""Ways"", ','), 1) 
                END) > {1}",

            "Microsoft.EntityFrameworkCore.Sqlite" => @"
                SELECT * FROM UserRememberUseSettings
                WHERE INSTR(WeekDays, {0}) > 0 
                AND (CASE 
                    WHEN Ways IS NULL OR Ways = '' THEN 0 
                    ELSE LENGTH(Ways) - LENGTH(REPLACE(Ways, ',', '')) + 1 
                END) > {1}",

            _ => throw new NotSupportedException($"Database provider {databaseProvider} is not supported")
        };

        return await repository.Context.UserRememberUseSettings
            .FromSqlRaw(sql, dayOfWeekString, minWaysCount).ToListAsync();
    }
}