using Fin.Application.People;
using Fin.Application.People.Dtos;
using Fin.Application.People.Enums;
using Fin.Domain.People.Dtos;
using Fin.Domain.People.Entities;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Wallets.Dtos;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace Fin.Test.People;

public class PersonServiceTest : TestUtils.BaseTestWithContext
{
    #region Get

    [Fact]
    public async Task Get_ShouldReturnPerson_WhenExists()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(person, autoSave: true);

        // Act
        var result = await service.Get(person.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(person.Id);
        result.Name.Should().Be(TestUtils.Strings[0]);
        result.Inactivated.Should().BeFalse();
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Get(TestUtils.Guids[0]);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedResult_WhenHasData()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        await resources.Repository.AddAsync(
            new Person(new PersonInput { Name = "Person A" }), autoSave: true);
        await resources.Repository.AddAsync(
            new Person(new PersonInput { Name = "Person B" }), autoSave: true);

        var input = new PersonGetListInput { MaxResultCount = 10, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetList_ShouldReturnEmpty_WhenNoData()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = new PersonGetListInput { MaxResultCount = 10, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetList_ShouldOrderByInactivatedThenName()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var personC = new Person(new PersonInput { Name = "C Person" });
        var personA = new Person(new PersonInput { Name = "A Person" });
        var personB = new Person(new PersonInput { Name = "B Person" });
        personB.ToggleInactivated();

        await resources.Repository.AddAsync(personC, autoSave: true);
        await resources.Repository.AddAsync(personA, autoSave: true);
        await resources.Repository.AddAsync(personB, autoSave: true);

        var input = new PersonGetListInput { MaxResultCount = 10, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Items.Should().HaveCount(3);
        result.Items[0].Name.Should().Be("A Person");
        result.Items[0].Inactivated.Should().BeFalse();
        result.Items[1].Name.Should().Be("C Person");
        result.Items[1].Inactivated.Should().BeFalse();
        result.Items[2].Name.Should().Be("B Person");
        result.Items[2].Inactivated.Should().BeTrue();
    }

    [Fact]
    public async Task GetList_ShouldFilterByInactivated_WhenTrue()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var activePerson = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        var inactivePerson = new Person(new PersonInput { Name = TestUtils.Strings[1] });
        inactivePerson.ToggleInactivated();

        await resources.Repository.AddAsync(activePerson, autoSave: true);
        await resources.Repository.AddAsync(inactivePerson, autoSave: true);

        var input = new PersonGetListInput 
        { 
            MaxResultCount = 10, 
            SkipCount = 0,
            Inactivated = true 
        };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be(TestUtils.Strings[1]);
        result.Items[0].Inactivated.Should().BeTrue();
    }

    [Fact]
    public async Task GetList_ShouldFilterByInactivated_WhenFalse()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var activePerson = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        var inactivePerson = new Person(new PersonInput { Name = TestUtils.Strings[1] });
        inactivePerson.ToggleInactivated();

        await resources.Repository.AddAsync(activePerson, autoSave: true);
        await resources.Repository.AddAsync(inactivePerson, autoSave: true);

        var input = new PersonGetListInput 
        { 
            MaxResultCount = 10, 
            SkipCount = 0,
            Inactivated = false 
        };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Items.Should().HaveCount(1);
        result.Items[0].Name.Should().Be(TestUtils.Strings[0]);
        result.Items[0].Inactivated.Should().BeFalse();
    }

    [Fact]
    public async Task GetList_ShouldReturnAll_WhenInactivatedIsNull()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var activePerson = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        var inactivePerson = new Person(new PersonInput { Name = TestUtils.Strings[1] });
        inactivePerson.ToggleInactivated();

        await resources.Repository.AddAsync(activePerson, autoSave: true);
        await resources.Repository.AddAsync(inactivePerson, autoSave: true);

        var input = new PersonGetListInput 
        { 
            MaxResultCount = 10, 
            SkipCount = 0,
            Inactivated = null 
        };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Items.Should().HaveCount(2);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldReturnSuccess_WhenInputIsValid()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = new PersonInput { Name = TestUtils.Strings[0] };

        // Act
        var result = await service.Create(input, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data.Name.Should().Be(TestUtils.Strings[0]);
        result.Data.Inactivated.Should().BeFalse();

        var dbPerson = await resources.Repository.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == result.Data.Id);
        
        dbPerson.Should().NotBeNull();
        dbPerson.Name.Should().Be(TestUtils.Strings[0]);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenNameIsRequired()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = new PersonInput { Name = null };

        // Act
        var result = await service.Create(input, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonCreateOrUpdateErrorCode.NameIsRequired);
        result.Data.Should().BeNull();

        var count = await resources.Repository.AsNoTracking().CountAsync();
        count.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Create_ShouldReturnFailure_WhenNameIsNullOrWhiteSpace(string name)
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = new PersonInput { Name = name };

        // Act
        var result = await service.Create(input, autoSave: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonCreateOrUpdateErrorCode.NameIsRequired);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenNameTooLong()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = new PersonInput { Name = new string('A', 101) };

        // Act
        var result = await service.Create(input, autoSave: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonCreateOrUpdateErrorCode.NameTooLong);
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenNameAlreadyInUse()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var existingPerson = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(existingPerson, autoSave: true);

        var input = new PersonInput { Name = TestUtils.Strings[0] };

        // Act
        var result = await service.Create(input, autoSave: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonCreateOrUpdateErrorCode.NameAlreadyInUse);
        result.Message.Should().Be("Name is already in use.");
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldReturnSuccess_WhenInputIsValid()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(person, autoSave: true);

        var input = new PersonInput { Name = TestUtils.Strings[1] };

        // Act
        var result = await service.Update(person.Id, input, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        var dbPerson = await resources.Repository.AsNoTracking()
            .FirstAsync(p => p.Id == person.Id);
        
        dbPerson.Name.Should().Be(TestUtils.Strings[1]);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenPersonNotFound()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = new PersonInput { Name = TestUtils.Strings[0] };

        // Act
        var result = await service.Update(TestUtils.Guids[0], input, autoSave: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonCreateOrUpdateErrorCode.PersonNotFound);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Update_ShouldReturnFailure_WhenNameIsNullOrWhiteSpace(string name)
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(person, autoSave: true);

        var input = new PersonInput { Name = name };

        // Act
        var result = await service.Update(person.Id, input, autoSave: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonCreateOrUpdateErrorCode.NameIsRequired);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenNameTooLong()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(person, autoSave: true);

        var input = new PersonInput { Name = new string('A', 101) };

        // Act
        var result = await service.Update(person.Id, input, autoSave: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonCreateOrUpdateErrorCode.NameTooLong);
    }

    [Fact]
    public async Task Update_ShouldReturnFailure_WhenNameAlreadyInUseByOther()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person1 = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        var person2 = new Person(new PersonInput { Name = TestUtils.Strings[1] });
        await resources.Repository.AddAsync(person1, autoSave: true);
        await resources.Repository.AddAsync(person2, autoSave: true);

        var input = new PersonInput { Name = TestUtils.Strings[0] };

        // Act
        var result = await service.Update(person2.Id, input, autoSave: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonCreateOrUpdateErrorCode.NameAlreadyInUse);
    }

    [Fact]
    public async Task Update_ShouldReturnSuccess_WhenNameUnchanged()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(person, autoSave: true);

        var input = new PersonInput { Name = TestUtils.Strings[0] };

        // Act
        var result = await service.Update(person.Id, input, autoSave: true);

        // Assert
        result.Success.Should().BeTrue();
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenPersonExists()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(person, autoSave: true);

        // Act
        var result = await service.Delete(person.Id, autoSave: true);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Data.Should().BeTrue();

        var dbPerson = await resources.Repository.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == person.Id);
        
        dbPerson.Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenPersonNotFound()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Delete(TestUtils.Guids[0], autoSave: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonDeleteErrorCode.PersonNotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenPersonInUse()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = new Wallet(new WalletInput
        {
            Name = TestUtils.Strings[4],
            Color = TestUtils.Strings[1],
            Icon = TestUtils.Strings[1],
        });
        var title = new Title
        {
            Id = TestUtils.Guids[1],
            Description = TestUtils.Strings[3],
            Wallet = wallet,
            Value = 10.0m,
        };
        await GetRepository<Wallet>().AddAsync(wallet, autoSave: true);
        await GetRepository<Title>().AddAsync(title, autoSave: true);
        
        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(person, autoSave: true);

        // Simulate TitlePerson relationship
        var titlePerson = new TitlePerson(title.Id, new TitlePersonInput{ PersonId = person.Id, Percentage = 100m });
        person.TitlePeople.Add(titlePerson);
        await resources.Repository.UpdateAsync(person, autoSave: true);

        // Act
        var result = await service.Delete(person.Id, autoSave: true);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorCode.Should().Be(PersonDeleteErrorCode.PersonInUse);

        var dbPerson = await resources.Repository.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == person.Id);
        
        dbPerson.Should().NotBeNull();
    }

    #endregion

    #region ToggleInactive

    [Fact]
    public async Task ToggleInactive_ShouldReturnTrue_WhenPersonExists()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(person, autoSave: true);
        person.Inactivated.Should().BeFalse();

        // Act
        var result = await service.ToggleInactive(person.Id, autoSave: true);

        // Assert
        result.Should().BeTrue();

        var dbPerson = await resources.Repository.AsNoTracking()
            .FirstAsync(p => p.Id == person.Id);
        
        dbPerson.Inactivated.Should().BeTrue();
    }

    [Fact]
    public async Task ToggleInactive_ShouldToggleBackToFalse()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var person = new Person(new PersonInput { Name = TestUtils.Strings[0] });
        await resources.Repository.AddAsync(person, autoSave: true);

        await service.ToggleInactive(person.Id, autoSave: true);
        var dbPerson1 = await resources.Repository.AsNoTracking()
            .FirstAsync(p => p.Id == person.Id);
        dbPerson1.Inactivated.Should().BeTrue();

        // Act
        var result = await service.ToggleInactive(person.Id, autoSave: true);

        // Assert
        result.Should().BeTrue();

        var dbPerson2 = await resources.Repository.AsNoTracking()
            .FirstAsync(p => p.Id == person.Id);
        
        dbPerson2.Inactivated.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReturnFalse_WhenPersonNotFound()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.ToggleInactive(TestUtils.Guids[0], autoSave: true);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    private PersonService GetService(Resources resources)
    {
        return new PersonService(resources.Repository);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            Repository = GetRepository<Person>()
        };
    }

    private class Resources
    {
        public IRepository<Person> Repository { get; set; }
    }
}