using Fin.Application.Tenants.Services;
using Fin.Infrastructure.BackgroundJobs;
using Fin.Infrastructure.Constants;
using Hangfire;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.Tenants.Backgrounds;

/// Portfolio-demo safeguard: wipes every tenant without an admin user once a day,
/// so visitor-entered data (and the account that created it) never lingers.
/// Only ever runs when ApiSettings:DemoMode is true, since it is destructive.
public class TenantCleanupBackgroundJob(
    ITenantCleanupService tenantCleanupService,
    IConfiguration configuration) : IAsyncRecurringBackgroundJob
{
    public string CronExpression => Cron.Daily(3, 0);
    public string RecurringJobId => "DailyNonAdminTenantCleanupJob";

    public async Task ExecuteAsync()
    {
        if (!configuration.GetValue<bool>(AppConstants.DemoModeConfigKey))
            return;

        await tenantCleanupService.DeleteNonAdminTenantsAsync();
    }
}
