using Fin.Application.Titles.Enums;
using Fin.Application.Titles.Validations.UpdateOrCrestes;
using Fin.Domain.People.Dtos;
using Fin.Domain.People.Entities;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.Titles.Validations;

public class TitleInputPeopleValidationTest : TestUtils.BaseTestWithContext
{
    private TitleInput GetValidInput() => new()
    {
        Description = TestUtils.Strings[0],
        Value = TestUtils.Decimals[0],
        Date = TestUtils.UtcDateTimes[0],
        WalletId = TestUtils.Guids[0],
        Type = TitleType.Income,
        TitleCategoriesIds = new List<Guid>(),
        TitlePeople = new List<TitlePersonInput>()
    };

    private async Task<Person> CreatePersonInDatabase(
        Resources resources,
        Guid id,
        string name,
        bool inactivated = false)
    {
        var person = new Person(new PersonInput
        {
            Name = name
        });
        
        person.Id = id;
        
        if (inactivated != person.Inactivated)
            person.ToggleInactivated();

        await resources.PersonRepository.AddAsync(person, autoSave: true);
        return person;
    }
    
    private async Task<Title> CreateTitleInDatabase(
        Resources resources, 
        Guid id, 
        List<Person> people)
    {
        var input = new TitleInput
        {
            Value = TestUtils.Decimals[0],
            Date = TestUtils.UtcDateTimes[0],
            Type = TitleType.Income,
            Description = TestUtils.Strings[0],
            TitleCategoriesIds = new List<Guid>(),
            TitlePeople = people.Select(p => new TitlePersonInput 
            { 
                PersonId = p.Id, 
                Percentage = 50m 
            }).ToList()
        };
        
        var title = new Title(input, 0m);
        title.Wallet = TestUtils.Wallets[0];
        title.Id = id;
        await resources.TitleRepository.AddAsync(title, autoSave: true);
        
        // Eager load TitlePeople for verification
        return await resources.TitleRepository
            .Include(t => t.TitlePeople)
            .FirstAsync(t => t.Id == id);
    }

    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenPeopleAreValid()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var person2 = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2]);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = 60m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2.Id, Percentage = 40m });

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Code.Should().BeNull();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenNoPeople()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = GetValidInput();
        // TitlePeople is empty

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenSplitIs100Percent()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var person2 = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2]);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = 50m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2.Id, Percentage = 50m });

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSomePeopleNotFound()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var notFoundId = TestUtils.Guids[9];

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = 50m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = notFoundId, Percentage = 50m });

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.SomePeopleNotFound);
        result.Data.Should().HaveCount(1);
        result.Data.Should().BeEquivalentTo(new List<Guid> { notFoundId });
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenMultiplePeopleNotFound()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var notFoundId1 = TestUtils.Guids[8];
        var notFoundId2 = TestUtils.Guids[9];

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = notFoundId1, Percentage = 50m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = notFoundId2, Percentage = 50m });

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.SomePeopleNotFound);
        result.Data.Should().HaveCount(2);
        result.Data.Should().BeEquivalentTo(new List<Guid> { notFoundId1, notFoundId2 });
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSomePeopleInactiveOnCreate()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var person2_Inactive = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2], inactivated: true);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = 50m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2_Inactive.Id, Percentage = 50m });

        // Act
        var result = await service.ValidateAsync(input, editingId: null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.SomePeopleInactive);
        result.Data.Should().HaveCount(1);
        result.Data.Should().BeEquivalentTo(new List<Guid> { person2_Inactive.Id });
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenPersonIsInactiveButAlreadyOnTitle()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1_Inactive = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1], inactivated: true);
        var title = await CreateTitleInDatabase(resources, TestUtils.Guids[5], new List<Person> { person1_Inactive });
        
        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1_Inactive.Id, Percentage = 100m });

        // Act
        var result = await service.ValidateAsync(input, editingId: title.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenAddingNewInactivePersonOnUpdate()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1_Active = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var person2_Inactive = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2], inactivated: true);
        var title = await CreateTitleInDatabase(resources, TestUtils.Guids[5], new List<Person> { person1_Active });
        
        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1_Active.Id, Percentage = 50m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2_Inactive.Id, Percentage = 50m }); // Adding a new inactive person

        // Act
        var result = await service.ValidateAsync(input, editingId: title.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.SomePeopleInactive);
        result.Data.Should().HaveCount(1);
        result.Data.Should().BeEquivalentTo(new List<Guid> { person2_Inactive.Id });
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSplitExceeds100Percent()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var person2 = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2]);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = 60m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2.Id, Percentage = 50m }); // Total: 110%

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.PeopleSplitRange);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSplitIsNegative()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var person2 = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2]);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = -60m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2.Id, Percentage = 50m }); // Total: 10% (but has negative)

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.PeopleSplitRange);
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenSplitIsLessThan0Dot01()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var person2 = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2]);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = 0m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2.Id, Percentage = 0.009m }); // Total: 10% (but has negative)

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.PeopleSplitRange);
        result.Data.Should().BeNull();
    }
    
    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenSplitIs0Dot01()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var person2 = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2]);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = 0m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2.Id, Percentage = 0.01m }); // Total: 10% (but has negative)

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Code.Should().BeNull();
        result.Data.Should().BeNull();
    }

    
    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenSplitIsLessThan100Percent()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);
        var person2 = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2]);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = 30m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2.Id, Percentage = 40m }); // Total: 70%

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenOnlyOnePersonWith100Percent()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1]);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1.Id, Percentage = 100m });

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenMultipleInactivePeople()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1_Inactive = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1], inactivated: true);
        var person2_Inactive = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2], inactivated: true);

        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1_Inactive.Id, Percentage = 50m });
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2_Inactive.Id, Percentage = 50m });

        // Act
        var result = await service.ValidateAsync(input, editingId: null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.SomePeopleInactive);
        result.Data.Should().HaveCount(2);
        result.Data.Should().BeEquivalentTo(new List<Guid> { person1_Inactive.Id, person2_Inactive.Id });
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenKeepingInactivePeopleAndAddingActiveOnes()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1_Inactive = await CreatePersonInDatabase(resources, TestUtils.Guids[1], TestUtils.Strings[1], inactivated: true);
        var person2_Active = await CreatePersonInDatabase(resources, TestUtils.Guids[2], TestUtils.Strings[2]);
        var title = await CreateTitleInDatabase(resources, TestUtils.Guids[5], new List<Person> { person1_Inactive });
        
        var input = GetValidInput();
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person1_Inactive.Id, Percentage = 50m }); // Already on title
        input.TitlePeople.Add(new TitlePersonInput { PersonId = person2_Active.Id, Percentage = 50m }); // New active person

        // Act
        var result = await service.ValidateAsync(input, editingId: title.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    #endregion

    private TitleInputPeopleValidation GetService(Resources resources)
    {
        return new TitleInputPeopleValidation(
            resources.TitleRepository,
            resources.PersonRepository
        );
    }

    private Resources GetResources()
    {
        return new Resources
        {
            TitleRepository = GetRepository<Title>(),
            PersonRepository = GetRepository<Person>()
        };
    }

    private class Resources
    {
        public IRepository<Title> TitleRepository { get; set; }
        public IRepository<Person> PersonRepository { get; set; }
    }
}