using Fin.Infrastructure.AmbientDatas;
using FluentAssertions;

namespace Fin.Test.AmbientDatas;

public class AmbientDataTest
{
    [Theory(DisplayName = "Testing AmbientData logged")]
    [InlineData(false)]
    [InlineData(true)]
    public void AmbientData_Logged(bool admin)
    {
        // Arrange
        var ambientData = new AmbientData();
        
        var userId = TestUtils.Guids[0];
        var tenantId = TestUtils.Guids[1];
        var displayName = TestUtils.Strings[0];
        
        // Act
        ambientData.SetData(tenantId, userId, displayName, admin);
        
        // Assert
        ambientData.IsLogged.Should().BeTrue();
        ambientData.IsAdmin.Should().Be(admin);

        ambientData.UserId.Should().Be(userId);
        ambientData.TenantId.Should().Be(tenantId);
        ambientData.DisplayName.Should().Be(displayName);
    }
    
    [Fact(DisplayName = "Testing AmbientData not logged")]
    public void AmbientData_NotLogged()
    {
        // Arrange
        var ambientData = new AmbientData();
        
        // Act
        ambientData.SetNotLogged();
        
        // Assert
        ambientData.IsLogged.Should().BeFalse();
        ambientData.IsAdmin.Should().BeFalse();

        ambientData.UserId.Should().Be(null);
        ambientData.TenantId.Should().Be(null);
        ambientData.DisplayName.Should().Be(null);
    }
}