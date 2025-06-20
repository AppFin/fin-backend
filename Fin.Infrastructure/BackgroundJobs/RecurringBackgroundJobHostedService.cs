using System.Linq.Expressions;
using System.Reflection;
using Hangfire;
using Hangfire.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.BackgroundJobs;

public class RecurringBackgroundJobHostedService(
    IServiceProvider serviceProvider,
    ILogger<RecurringBackgroundJobHostedService> logger
    ): IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Start adding recurring jobs");

        await using var scope = serviceProvider.CreateAsyncScope();

        var jobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var jobs = scope.ServiceProvider.GetServices<IAsyncRecurringBackgroundJob>();

        foreach (var job in jobs)
        {
            var jobType = job.GetType();
            try
            {

                jobManager.AddOrUpdate(
                    job.RecurringJobId,
                    () => ExecuteJobAsync(jobType),
                    job.CronExpression,
                    new RecurringJobOptions());

                logger.LogInformation($"Recurring job '{job.RecurringJobId}' registrado com sucesso. Cron: {job.CronExpression}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, $"Erro ao registrar o recurring job da classe {jobType.Name}");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    public async Task ExecuteJobAsync(Type jobType)
    {
        var job = (IAsyncRecurringBackgroundJob)serviceProvider.GetRequiredService(jobType);
        await job.ExecuteAsync();
    }

}