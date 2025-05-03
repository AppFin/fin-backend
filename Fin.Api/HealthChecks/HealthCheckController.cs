using Fin.Application.HealthChecks.Dtos;
using Fin.Application.HealthChecks.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fin.Api.HealthChecks;

[Route("health")]
public class HealthCheckController(IHealthCheckService healthCheckService) : ControllerBase
{
    [HttpGet]
    public ActionResult<HealthCheckOutput> Get()
    {
        return healthCheckService.GetHealthCheck();
    }
}