using Fin.Application.Users.Services;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.BackgroundJobs;
using Hangfire;

namespace Fin.Application.Users.Backgrounds;

public class UserDeleteBackgroundJobs(IUserDeleteService userDeleteService): IAsyncRecurringBackgroundJob
{
    public string CronExpression => Cron.Daily(0, 0);
    public string RecurringJobId => "MidNightDeleteUserJob";
    public async Task ExecuteAsync()
    {
        await userDeleteService.EffectiveDeleteUsers();
    }
}