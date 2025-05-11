using Fin.Application.Globals.Services;
using FluentAssertions;
using Moq;

namespace Fin.Test.Globals.Services;

public class ConfirmationCodeGeneratorTests
{
    [Fact]
    public void Generate_Should_ReturnExpectedCode_WhenRandomIsControlled()
    {
        // Arrange
        var mock = new Mock<IRandomGenerator>();
        var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        
        mock.Setup(m => m.Next(chars.Length)).Returns(0);

        var generator = new ConfirmationCodeGenerator(mock.Object);

        // Act
        var code = generator.Generate();

        // Assert
        code.Should().Be("AAAAAA");
    }

    [Fact]
    public void Generate_Should_Return6Characters()
    {
        var mock = new Mock<IRandomGenerator>();
        mock.Setup(m => m.Next(It.IsAny<int>())).Returns(1);
        var generator = new ConfirmationCodeGenerator(mock.Object);
        var code = generator.Generate();

        code.Length.Should().Be(6);
        code.Should().Be("BBBBBB");
    }
}