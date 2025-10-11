using Fin.Application.FinancialInstitutions;
using Fin.Application.FinancialInstitutions.Dtos;
using Fin.Domain.FinancialInstitutions.Dtos;
using Fin.Domain.FinancialInstitutions.Enums;
using Fin.Domain.FinancialInstitutions.Entities;
using Fin.Infrastructure.Database.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Fin.Domain.Wallets.Entities;
using Fin.Domain.Wallets.Dtos;

namespace Fin.Test.FinancialInstitutions.Services;

public class FinancialInstitutionServiceTest : TestUtils.BaseTestWithContext
{
    private FinancialInstitutionService GetService(Resources resources)
    {
        return new FinancialInstitutionService(resources.FinancialInstitutionRepository);
    }

    private Resources GetResources()
    {
        return new Resources
        {
            FinancialInstitutionRepository = GetRepository<FinancialInstitution>(),
            WalletRepository = GetRepository<Wallet>()
        };
    }

    private class Resources
    {
        public IRepository<FinancialInstitution> FinancialInstitutionRepository { get; set; }
        public IRepository<Wallet> WalletRepository { get; set; }
    }

    // Helper for valid input
    private FinancialInstitutionInput GetValidInput(string name = "Test FI") => new()
    {
        Name = name,
        Code = "123",
        Type = FinancialInstitutionType.Bank,
        Icon = "fa-bank",
        Color = "#123456"
    };

    #region Get

    [Fact]
    public async Task Get_ShouldReturnFinancialInstitution_WhenExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        var institution = new FinancialInstitution(input);
        await resources.FinancialInstitutionRepository.AddAsync(institution, true);

        // Act
        var result = await service.Get(institution.Id);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(institution.Id);
        result.Name.Should().Be(institution.Name);
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Get(TestUtils.Guids[9]);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetList

    [Fact]
    public async Task GetList_ShouldReturnPagedResult_WithSortingAndPaging()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        await resources.FinancialInstitutionRepository.AddAsync(new FinancialInstitution(GetValidInput("Z")), true);
        await resources.FinancialInstitutionRepository.AddAsync(new FinancialInstitution(GetValidInput("A")), true);
        await resources.FinancialInstitutionRepository.AddAsync(new FinancialInstitution(GetValidInput("M")), true);

        var input = new FinancialInstitutionGetListInput { MaxResultCount = 2, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(2);
        // Default sort is Inactive (false first) then Name (asc).
        result.Items.First().Name.Should().Be("A");
        result.Items.Last().Name.Should().Be("M");
    }

    [Fact]
    public async Task GetList_ShouldFilterByInactiveAndType()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Active Bank
        await resources.FinancialInstitutionRepository.AddAsync(new FinancialInstitution(GetValidInput("Bank1")), true);
        
        // Inactive Bank
        var inactiveBank = new FinancialInstitution(GetValidInput("Bank2"));
        inactiveBank.ToggleInactive();
        await resources.FinancialInstitutionRepository.AddAsync(inactiveBank, true);
        
        // Active DigitalBank
        var validInput = GetValidInput("Digital1");
        validInput.Type = FinancialInstitutionType.DigitalBank;
        await resources.FinancialInstitutionRepository.AddAsync(new FinancialInstitution(validInput), true);

        var input = new FinancialInstitutionGetListInput { Inactive = true, Type = FinancialInstitutionType.Bank, MaxResultCount = 10, SkipCount = 0 };

        // Act
        var result = await service.GetList(input);

        // Assert
        result.Should().NotBeNull();
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Bank2");
        result.Items.First().Inactive.Should().BeTrue();
        result.Items.First().Type.Should().Be(FinancialInstitutionType.Bank);
    }

    #endregion

    #region Create

    [Fact]
    public async Task Create_ShouldAddInstitutionAndReturnOutput()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();

        // Act
        var result = await service.Create(input, true);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be(input.Name);
        var dbInstitution = await resources.FinancialInstitutionRepository.Query(false).FirstOrDefaultAsync(a => a.Id == result.Id);
        dbInstitution.Should().NotBeNull();
        dbInstitution.Name.Should().Be(input.Name);
    }

    [Fact]
    public async Task Create_ShouldThrowException_WhenNameIsMissing()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Name = null;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            async () => await service.Create(input, true)
        );
        exception.Message.Should().Be("Name is required");
    }

    [Fact]
    public async Task Create_ShouldThrowException_WhenNameAlreadyExists()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var name = TestUtils.Strings[0];
        await resources.FinancialInstitutionRepository.AddAsync(new FinancialInstitution(GetValidInput(name)), true);
        var input = GetValidInput(name); // Duplicate name

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            async () => await service.Create(input, true)
        );

        exception.Message.Should().Be("A financial institution with this name already exists");
    }

    [Fact]
    public async Task Create_ShouldThrowException_WhenIconIsTooLong()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var input = GetValidInput();
        input.Icon = new string('x', 21);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            async () => await service.Create(input, true)
        );

        exception.Message.Should().Be("Icon must be at most 20 characters long");
    }

    #endregion

    #region Update

    [Fact]
    public async Task Update_ShouldModifyInstitutionAndReturnTrue()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var existingName = TestUtils.Strings[0];
        var institution = new FinancialInstitution(GetValidInput(existingName));
        await resources.FinancialInstitutionRepository.AddAsync(institution, true);

        var input = GetValidInput(TestUtils.Strings[1]);
        input.Code = "999";

        // Act
        var result = await service.Update(institution.Id, input, true);

        // Assert
        result.Should().BeTrue();
        var dbInstitution = await resources.FinancialInstitutionRepository.Query(false).FirstAsync(f => f.Id == institution.Id);
        dbInstitution.Name.Should().Be(input.Name);
        dbInstitution.Code.Should().Be("999");
    }

    [Fact]
    public async Task Update_ShouldReturnFalse_WhenInstitutionNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var nonExistentId = TestUtils.Guids[9];

        // Act
        var result = await service.Update(nonExistentId, GetValidInput(), true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldThrowException_WhenNameConflict()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        
        // Institution A (Target name conflict)
        await resources.FinancialInstitutionRepository.AddAsync(new FinancialInstitution(GetValidInput("Bank A")), true);
        
        // Institution B (Institution to update)
        var institutionB = new FinancialInstitution(GetValidInput("Bank B"));
        await resources.FinancialInstitutionRepository.AddAsync(institutionB, true);

        var input = GetValidInput("Bank A"); // Attempt to use A's name

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadHttpRequestException>(
            async () => await service.Update(institutionB.Id, input, true)
        );

        exception.Message.Should().Be("A financial institution with this name already exists");
    }

    [Fact]
    public async Task Update_ShouldSucceed_WhenNameIsUnchanged()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var name = TestUtils.Strings[0];
        var institution = new FinancialInstitution(GetValidInput(name));
        await resources.FinancialInstitutionRepository.AddAsync(institution, true);

        var input = GetValidInput(name);
        input.Color = "#AAAAAA";

        // Act
        var result = await service.Update(institution.Id, input, true);

        // Assert
        result.Should().BeTrue();
        var dbInstitution = await resources.FinancialInstitutionRepository.Query(false).FirstAsync(f => f.Id == institution.Id);
        dbInstitution.Color.Should().Be("#AAAAAA");
    }

    #endregion

    #region Delete

    [Fact]
    public async Task Delete_ShouldReturnTrue_WhenInstitutionDeleted()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var institution = new FinancialInstitution(GetValidInput());
        await resources.FinancialInstitutionRepository.AddAsync(institution, true);

        // Act
        var result = await service.Delete(institution.Id, true);

        // Assert
        result.Should().BeTrue();
        (await resources.FinancialInstitutionRepository.Query(false).FirstOrDefaultAsync(f => f.Id == institution.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_ShouldReturnFalse_WhenInstitutionNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.Delete(TestUtils.Guids[9], true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldReturnFalse_WhenInstitutionHasWallets()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var institution = new FinancialInstitution(GetValidInput());
        await resources.FinancialInstitutionRepository.AddAsync(institution, true);

        // Add a related wallet
        await resources.WalletRepository.AddAsync(new Wallet(new WalletInput { Name = "W1", Color = "#FFF", Icon = "I", InitialBalance = 0m, FinancialInstitutionId = institution.Id }), true);

        // Act
        var result = await service.Delete(institution.Id, true);

        // Assert
        result.Should().BeFalse();
        (await resources.FinancialInstitutionRepository.Query(false).FirstOrDefaultAsync(f => f.Id == institution.Id)).Should().NotBeNull();
    }

    #endregion

    #region ToggleInactive

    [Fact]
    public async Task ToggleInactive_ShouldReturnTrue_AndDeactivate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var institution = new FinancialInstitution(GetValidInput());
        await resources.FinancialInstitutionRepository.AddAsync(institution, true);
        institution.Inactive.Should().BeFalse();

        // Act
        var result = await service.ToggleInactive(institution.Id, true);

        // Assert
        result.Should().BeTrue();
        var dbInstitution = await resources.FinancialInstitutionRepository.Query(false).FirstAsync(f => f.Id == institution.Id);
        dbInstitution.Inactive.Should().BeTrue();
    }
    
    [Fact]
    public async Task ToggleInactive_ShouldReturnTrue_AndReactivate()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var institution = new FinancialInstitution(GetValidInput());
        institution.ToggleInactive(); // Initial state: Inactive
        await resources.FinancialInstitutionRepository.AddAsync(institution, true);
        institution.Inactive.Should().BeTrue();

        // Act
        var result = await service.ToggleInactive(institution.Id, true);

        // Assert
        result.Should().BeTrue();
        var dbInstitution = await resources.FinancialInstitutionRepository.Query(false).FirstAsync(f => f.Id == institution.Id);
        dbInstitution.Inactive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReturnFalse_WhenInstitutionNotFound()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);

        // Act
        var result = await service.ToggleInactive(TestUtils.Guids[9], true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReturnFalse_WhenHasActiveWallets()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var institution = new FinancialInstitution(GetValidInput()); // Active
        await resources.FinancialInstitutionRepository.AddAsync(institution, true);
        
        // Add an active related wallet
        await resources.WalletRepository.AddAsync(new Wallet(new WalletInput { Name = "W1", Color = "#FFF", Icon = "I", InitialBalance = 0m, FinancialInstitutionId = institution.Id }), true);

        // Act
        var result = await service.ToggleInactive(institution.Id, true);

        // Assert
        result.Should().BeFalse();
        // Verify institution status did not change
        var dbInstitution = await resources.FinancialInstitutionRepository.Query(false).FirstAsync(f => f.Id == institution.Id);
        dbInstitution.Inactive.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleInactive_ShouldReturnTrue_WhenHasOnlyInactiveWallets()
    {
        // Arrange
        var resources = GetResources();
        var service = GetService(resources);
        var institution = new FinancialInstitution(GetValidInput()); // Active
        await resources.FinancialInstitutionRepository.AddAsync(institution, true);

        // Add an inactive related wallet
        var inactiveWallet = new Wallet(new WalletInput { Name = "W1", Color = "#FFF", Icon = "I", InitialBalance = 0m, FinancialInstitutionId = institution.Id });
        inactiveWallet.ToggleInactivated();
        await resources.WalletRepository.AddAsync(inactiveWallet, true);

        // Act
        var result = await service.ToggleInactive(institution.Id, true); // Deactivating institution

        // Assert
        result.Should().BeTrue();
        // Verify institution status changed
        var dbInstitution = await resources.FinancialInstitutionRepository.Query(false).FirstAsync(f => f.Id == institution.Id);
        dbInstitution.Inactive.Should().BeTrue();
    }

    #endregion
}