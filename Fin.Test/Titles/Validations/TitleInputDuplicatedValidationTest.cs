using Fin.Application.Titles.Enums;
using Fin.Application.Titles.Validations.UpdateOrCrestes;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;

namespace Fin.Test.Titles.Validations;

public class TitleInputDuplicatedValidationTest : TestUtils.BaseTestWithContext
{
    private TitleInput GetValidInput(DateTime date) => new()
    {
        Description = TestUtils.Strings[0],
        Value = TestUtils.Decimals[0],
        Date = date,
        WalletId = TestUtils.Guids[0],
        Type = TitleType.Income
    };

    private async Task<Title> CreateTitleInDatabase(
        Resources resources,
        TitleInput input,
        Guid? id = null)
    {
        var title = new Title(input, 0m);
        var wallet = TestUtils.Wallets[0];
        wallet.Id = title.WalletId;
        title.Wallet = wallet;
        if (id.HasValue)
            title.Id = id.Value;
        
        await resources.TitleRepository.AddAsync(title, autoSave: true);
        return title;
    }

    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenTitleIsUnique()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);
        
        var date = TestUtils.UtcDateTimes[0];
        var input = GetValidInput(date);

        // Act
        var result = await service.ValidateAsync(input);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenDuplicateExistsOnCreate()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var date = TestUtils.UtcDateTimes[0];
        var input = GetValidInput(date);
        
        await CreateTitleInDatabase(resources, input);

        // Act
        var result = await service.ValidateAsync(input, editingId: null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.DuplicateTitleInSameDateTimeMinute);
    }
    
    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenDuplicateExistsOnUpdateForSameEntity()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var date = TestUtils.UtcDateTimes[0];
        var input = GetValidInput(date);
        
        var existingTitle = await CreateTitleInDatabase(resources, input, TestUtils.Guids[1]);

        // Act
        var result = await service.ValidateAsync(input, editingId: existingTitle.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }
    
    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenDuplicateExistsOnUpdateForAnotherEntity()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var date = TestUtils.UtcDateTimes[0];
        var input = GetValidInput(date);
        
        // Create the duplicate entry
        await CreateTitleInDatabase(resources, input, TestUtils.Guids[1]);
        
        // The entity being edited is different
        var editingId = TestUtils.Guids[2];

        // Act
        var result = await service.ValidateAsync(input, editingId: editingId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.DuplicateTitleInSameDateTimeMinute);
    }
    
    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenInputHasDifferentMinute()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var date1 = TestUtils.UtcDateTimes[0]; // e.g., 10:30:00
        var input1 = GetValidInput(date1);
        
        await CreateTitleInDatabase(resources, input1);
        
        var date2 = date1.AddMinutes(1); // e.g., 10:31:00
        var input2 = GetValidInput(date2);

        // Act
        var result = await service.ValidateAsync(input2, editingId: null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
    }

    #endregion

    private TitleInputDuplicatedValidation GetService(Resources resources)
    {
        return new TitleInputDuplicatedValidation(resources.TitleRepository);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            TitleRepository = GetRepository<Title>()
        };
    }

    private class Resources
    {
        public IRepository<Title> TitleRepository { get; set; }
    }
}