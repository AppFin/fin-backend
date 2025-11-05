using Fin.Application.Titles.Enums;
using Fin.Application.Titles.Validations.UpdateOrCrestes;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;

namespace Fin.Test.Titles.Validations;

public class TitleInputMustExistValidationTest : TestUtils.BaseTestWithContext
{
    private TitleInput GetValidInput() => new()
    {
        Description = TestUtils.Strings[0],
        Value = TestUtils.Decimals[0],
        Date = TestUtils.UtcDateTimes[0],
        WalletId = TestUtils.Guids[0],
        Type = TitleType.Income
    };

    private async Task<Title> CreateTitleInDatabase(Resources resources, Guid id)
    {
        var input = GetValidInput();
        
        var title = new Title(input, 0m);
        title.Id = id;
        var wallet = TestUtils.Wallets[0];
        wallet.Id = title.WalletId;
        title.Wallet = wallet;
        await resources.TitleRepository.AddAsync(title, autoSave: true);
        return title;
    }

    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenTitleExists()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var input = GetValidInput();
        var existingTitle = await CreateTitleInDatabase(resources, TestUtils.Guids[0]);

        // Act
        var result = await service.ValidateAsync(input, editingId: existingTitle.Id);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Code.Should().BeNull();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnFailure_WhenTitleDoesNotExist()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);
        
        var input = GetValidInput();
        var nonExistentId = TestUtils.Guids[9];

        // Act
        var result = await service.ValidateAsync(input, editingId: nonExistentId);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleCreateOrUpdateErrorCode.TitleNotFound);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenEditingIdIsNull()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);
        
        var input = GetValidInput();

        // Act
        var result = await service.ValidateAsync(input, editingId: null);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.Code.Should().BeNull();
    }
    
    #endregion

    private TitleInputMustExistValidation GetService(Resources resources)
    {
        return new TitleInputMustExistValidation(resources.TitleRepository);
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