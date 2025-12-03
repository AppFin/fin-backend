using System.Globalization;
using Fin.Domain.Tenants.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Localizations;

public class LocalizationMiddleware(RequestDelegate next, ILogger<LocalizationMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context, IAmbientData ambientData, IRepository<Tenant> tenantRepository)
    {
        if (ambientData.IsLogged)
        {
            await TrySetLocalizationByTenant(context, ambientData, tenantRepository);
        }
        else
        {
            TrySetLocalizationByHeader(context);
        }

        await next(context);
    }

    private void TrySetLocalizationByHeader(HttpContext context)
    {
        var locale = context.Request.Headers.AcceptLanguage.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(locale)) return;

        locale = locale[..5];

        try
        {
            var culture = new CultureInfo(locale);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            logger.LogWarning("Culture '{Locale}' not found from header. Falling back to en-US", locale);
            var fallback = new CultureInfo("en-US");
            CultureInfo.CurrentCulture = fallback;
            CultureInfo.CurrentUICulture = fallback;
        }
    }

    private async Task TrySetLocalizationByTenant(HttpContext context, IAmbientData ambientData,
        IRepository<Tenant> tenantRepository)
    {
        var tenant = await tenantRepository.AsNoTracking().FirstAsync(tenant => tenant.Id == ambientData.TenantId);
        var locale = tenant.Locale;

        if (string.IsNullOrWhiteSpace(locale))
        {
            TrySetLocalizationByHeader(context);
            return;
        }

        try
        {
            var culture = new CultureInfo(locale);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
        }
        catch (CultureNotFoundException)
        {
            logger.LogWarning("Culture '{Locale}' not found on tenantId: {TenantId}. Trying get on header", locale,
                tenant.Id);
            TrySetLocalizationByHeader(context);
        }
    }
}