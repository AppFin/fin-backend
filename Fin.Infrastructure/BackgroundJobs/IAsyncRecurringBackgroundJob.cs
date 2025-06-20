namespace Fin.Infrastructure.BackgroundJobs;

public interface IAsyncRecurringBackgroundJob
{
    string CronExpression { get; }
    string RecurringJobId { get; }

    Task ExecuteAsync();
}