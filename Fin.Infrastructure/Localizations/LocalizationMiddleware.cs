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
            var tenant = await tenantRepository.AsNoTracking().FirstAsync(tenant => tenant.Id == ambientData.TenantId);
            var locale = tenant.Locale;

            if (!string.IsNullOrWhiteSpace(locale))
            {
                try
                {
                    var culture = new CultureInfo(locale);
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                }
                catch (CultureNotFoundException)
                {
                    logger.LogWarning("Culture '{Locale}' not found. Falling back to 'en-US'. TenantId: {TenantId}", locale, tenant.Id);
                    var fallback = new CultureInfo("en-US");
                    CultureInfo.CurrentCulture = fallback;
                    CultureInfo.CurrentUICulture = fallback;
                }
            }
        }
        await next(context);
    }
}