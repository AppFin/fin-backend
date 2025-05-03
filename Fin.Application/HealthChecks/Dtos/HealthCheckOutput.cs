namespace Fin.Application.HealthChecks.Dtos;

public class HealthCheckOutput
{
    public string Status { get; set; }
    public string Version { get; set; }
    public DateTime Timestamp { get; set; }
}