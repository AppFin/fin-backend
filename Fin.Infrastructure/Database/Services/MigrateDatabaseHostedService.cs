using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Database.Services;

public class MigrateDatabaseHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MigrateDatabaseHostedService> _logger;

    public MigrateDatabaseHostedService(IServiceProvider serviceProvider, ILogger<MigrateDatabaseHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<FinDbContext>();
            try
            {
                _logger.LogInformation("Applying migrations...");
                await dbContext.Database.MigrateAsync(cancellationToken);
                _logger.LogInformation("Migrations applied successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"An error occurred while applying migrations: {ex.Message}");
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}