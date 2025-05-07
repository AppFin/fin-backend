using Fin.Api.HealthChecks;
using Fin.Application.HealthChecks.Dtos;
using Fin.Application.HealthChecks.Services;
using Moq;
using FluentAssertions;

namespace Fin.Test.HealthChecks;

public class HealthCheckControllerTest: TestUtils.BaseTest
{
    [Fact]
    public void Get()
    {
        // Arrange
        var fakeHealthCheck = new HealthCheckOutput
        {
            Status = TestUtils.Strings.First(),
            Version = TestUtils.Strings.Last(),
            Timestamp = TestUtils.UtcDateTimes.First()
        };
        
        var fakeService = new Mock<IHealthCheckService>();
        fakeService.Setup(a => a.GetHealthCheck()).Returns(fakeHealthCheck);
        
        var controller = new HealthCheckController(fakeService.Object);
        
        // Act
        var healthCheckResult = controller.Get();
        
        // Assert
        healthCheckResult.Value.Should().BeEquivalentTo(fakeHealthCheck);
        
    }
}