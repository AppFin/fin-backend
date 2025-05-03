using Fin.Application.AutoServices.Interfaces;
using Fin.Application.HealthChecks.Dtos;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.HealthChecks.Services;

public interface IHealthCheckService
{
    public HealthCheckOutput GetHealthCheck();
}

public class HealthCheckService: IHealthCheckService, IAutoSingleton
{
    private readonly IConfiguration _configuration;

    public HealthCheckService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public HealthCheckOutput GetHealthCheck()
    {
        return new HealthCheckOutput
        {
            Status = "OK",
            Version = _configuration["ApiSettings:Version"] ?? "",
            Timestamp = DateTime.Now
        };
    }
}