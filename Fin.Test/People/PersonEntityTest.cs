using Fin.Domain.People.Dtos;
using Fin.Domain.People.Entities;
using FluentAssertions;

namespace Fin.Test.People;

public class PersonEntityTest
{
    #region Constructor

    [Fact]
    public void Constructor_ShouldInitializeWithInput()
    {
        // Arrange
        var input = new PersonInput
        {
            Name = TestUtils.Strings[0]
        };

        // Act
        var person = new Person(input);

        // Assert
        person.Should().NotBeNull();
        person.Name.Should().Be(TestUtils.Strings[0]);
        person.Inactivated.Should().BeFalse();
    }

    [Fact]
    public void Constructor_ShouldInitializeWithEmptyCollections()
    {
        // Arrange
        var input = new PersonInput
        {
            Name = TestUtils.Strings[0]
        };

        // Act
        var person = new Person(input);

        // Assert
        person.Titles.Should().NotBeNull();
        person.Titles.Should().BeEmpty();
        person.TitlePeople.Should().NotBeNull();
        person.TitlePeople.Should().BeEmpty();
    }

    #endregion

    #region Update

    [Fact]
    public void Update_ShouldUpdateName()
    {
        // Arrange
        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        var updateInput = new PersonInput { Name = TestUtils.Strings[1] };

        // Act
        person.Update(updateInput);

        // Assert
        person.Name.Should().Be(TestUtils.Strings[1]);
    }

    [Fact]
    public void Update_ShouldNotChangeInactivatedStatus()
    {
        // Arrange
        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        person.ToggleInactivated();
        var updateInput = new PersonInput { Name = TestUtils.Strings[1] };

        // Act
        person.Update(updateInput);

        // Assert
        person.Name.Should().Be(TestUtils.Strings[1]);
        person.Inactivated.Should().BeTrue();
    }

    #endregion

    #region ToggleInactivated

    [Fact]
    public void ToggleInactivated_ShouldChangeFromFalseToTrue()
    {
        // Arrange
        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        person.Inactivated.Should().BeFalse();

        // Act
        person.ToggleInactivated();

        // Assert
        person.Inactivated.Should().BeTrue();
    }

    [Fact]
    public void ToggleInactivated_ShouldChangeFromTrueToFalse()
    {
        // Arrange
        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        person.ToggleInactivated();
        person.Inactivated.Should().BeTrue();

        // Act
        person.ToggleInactivated();

        // Assert
        person.Inactivated.Should().BeFalse();
    }

    [Fact]
    public void ToggleInactivated_ShouldToggleMultipleTimes()
    {
        // Arrange
        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });

        // Act & Assert
        person.Inactivated.Should().BeFalse();
        
        person.ToggleInactivated();
        person.Inactivated.Should().BeTrue();
        
        person.ToggleInactivated();
        person.Inactivated.Should().BeFalse();
        
        person.ToggleInactivated();
        person.Inactivated.Should().BeTrue();
    }

    #endregion
}