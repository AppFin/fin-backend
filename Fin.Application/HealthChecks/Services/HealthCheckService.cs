using Fin.Application.HealthChecks.Dtos;
using Fin.Infrastructure.AutoServices;
using Fin.Infrastructure.Services;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.HealthChecks.Services;

public interface IHealthCheckService
{
    public HealthCheckOutput GetHealthCheck();
}

public class HealthCheckService(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    : IHealthCheckService, IAutoSingleton
{
    public HealthCheckOutput GetHealthCheck()
    {
        return new HealthCheckOutput
        {
            Status = "OK",
            Version = configuration["ApiSettings:Version"] ?? "",
            Timestamp = dateTimeProvider.UtcNow()
        };
    }
}