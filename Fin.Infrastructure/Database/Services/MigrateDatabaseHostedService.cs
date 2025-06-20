using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Database.Services;

public class MigrateDatabaseHostedService(
    IServiceProvider serviceProvider,
    ILogger<MigrateDatabaseHostedService> logger)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinDbContext>();
            try
            {
                logger.LogInformation("Applying migrations...");
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Migrations applied successfully.");
            }
            catch (Exception ex)
            {
                logger.LogError($"An error occurred while applying migrations: {ex.Message}");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}