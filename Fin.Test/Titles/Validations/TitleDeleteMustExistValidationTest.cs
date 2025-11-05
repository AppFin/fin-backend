using Fin.Application.Titles.Enums;
using Fin.Application.Titles.Validations.Deletes;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;

namespace Fin.Test.Titles.Validations;

public class TitleDeleteMustExistValidationTest : TestUtils.BaseTestWithContext
{
    #region ValidateAsync

    [Fact]
    public async Task ValidateAsync_ShouldReturnSuccess_WhenTitleExists()
    {
        // Arrange
        await ConfigureLoggedAmbientAsync();
        var resources = GetResources();
        var service = GetService(resources);

        var wallet = TestUtils.Wallets[0];
        var titleId = TestUtils.Guids[0];
        var title = new Title(new TitleInput
        {
            Description = TestUtils.Strings[0],
            Value = TestUtils.Decimals[0],
            Date = TestUtils.UtcDateTimes[0],
            Type = TitleType.Income
        }, 0m);
        title.Id = titleId;
        title.Wallet = wallet;
        title.Wallet.Id = wallet.Id;
        await resources.TitleRepository.AddAsync(title, autoSave: true);

        // Act
        var result = await service.ValidateAsync(titleId, null, CancellationToken.None);

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

        var nonExistentTitleId = TestUtils.Guids[9];

        // Act
        var result = await service.ValidateAsync(nonExistentTitleId, null, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeFalse();
        result.Code.Should().Be(TitleDeleteErrorCode.TitleNotFound);
    }

    #endregion

    private TitleDeleteMustExistValidation GetService(Resources resources)
    {
        return new TitleDeleteMustExistValidation(resources.TitleRepository);
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