using Fin.Domain.Tenants.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Localizations;

public interface ILocalizationService
{
    public Task<string> FormatToUserDateTime(DateTime dateTime, bool showTime = true, bool showLong = false);
}

public class LocalizationService(IAmbientData ambientData, IRepository<Tenant> tenantsRepository, ILogger<LocalizationService> logger): ILocalizationService, IAutoTransient
{
    public async Task<string> FormatToUserDateTime(DateTime dateTime, bool showTime = true, bool showLong = false)
    {
        var formt = "f";
        if (!showTime)
        {
            formt = "d";
        }
        else if (!showLong)
        {
            formt = "g";
        }
        
        var timeZoneInfo = TimeZoneInfo.Utc;
        if (ambientData.IsLogged)
        {
            var tenant = await tenantsRepository.FirstAsync(t => t.Id == ambientData.TenantId);
            try
            {
                if (!string.IsNullOrWhiteSpace(tenant.Timezone))
                {
                    timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(tenant.Locale);
                    
                }
            }
            catch (TimeZoneNotFoundException)
            {
                logger.LogWarning("TimeZone {TimeZone} invalid for Tenant Id {TenantId}. Falling back to 'UTC'", tenant.Timezone, tenant.Id);
            }
        }
        
        var localDate = TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeZoneInfo);
        return localDate.ToString(formt);
    }
}