using Fin.Application.HealthChecks.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using FluentAssertions;

namespace Fin.Test.HealthChecks;

public class HealthCheckServiceTest: TestUtils.BaseTest
{
    [Fact]
    public void GetHealthCheck()
    {
        // Arrange
        var fakeVersion = TestUtils.Strings.First();
        var fakeDateTime = TestUtils.UtcDateTimes.First();
        
        var fakeConfiguration = new Mock<IConfiguration>();
        fakeConfiguration.Setup(a => a["ApiSettings:Version"]).Returns(fakeVersion);
        
        DateTimeProvider.Setup(a => a.UtcNow()).Returns(fakeDateTime);
        
        var service = new HealthCheckService(fakeConfiguration.Object, DateTimeProvider.Object);
        
        // Act
        var result = service.GetHealthCheck();
        // Assert
        
        result.Status.Should().Be("OK");
        result.Version.Should().Be(fakeVersion);
        result.Timestamp.Should().Be(fakeDateTime);
    }
}