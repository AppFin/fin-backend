using Fin.Application.Globals.Dtos;
using Fin.Application.Titles.Enums;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.TitleCategories.Enums;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Titles.Enums;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Services;

public interface ITitleValidationService
{
    public Task<ValidationResultDto<bool, TitleDeleteErrorCode>> ValidateDelete(Guid titleId);

    public Task<ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode>> ValidateInput<T>(TitleInput input,
        Guid? editingId = null);
}

public class TitleValidationService(
    IRepository<Title> titleRepository,
    IRepository<Wallet> walletRepository,
    IRepository<TitleCategory> categoryRepository
) : ITitleValidationService, IAutoTransient
{
    public async Task<ValidationResultDto<bool, TitleDeleteErrorCode>> ValidateDelete(Guid titleId)
    {
        var validation = new ValidationResultDto<bool, TitleDeleteErrorCode>();

        var title = await titleRepository.Query(tracking: false).FirstOrDefaultAsync(t => t.Id == titleId);
        if (title == null)
        {
            return validation.AddError(TitleDeleteErrorCode.TitleNotFound);
        }

        validation.Success = true;
        return validation;
    }

    public async Task<ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode>> ValidateInput<T>(TitleInput input, Guid? editingId = null)
    {
        var validation = new ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode>();

        var titleEditing = await ValidateTitleEditing(editingId, validation);
        if (!validation.Success) return validation;

        await ValidateBasicFields(input, validation);
        if (!validation.Success) return validation;

        await ValidateWallet(input, validation);
        if (!validation.Success) return validation;

        await ValidateDuplicateTitle(input, editingId, validation);
        if (!validation.Success) return validation;

        if (input.TitleCategoriesIds.Any())
        {
            await ValidateCategories(input, titleEditing, validation);
        }

        return validation;
    }

    private async Task<Title?> ValidateTitleEditing<T>(
        Guid? editingId,
        ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode> validation)
    {
        if (!editingId.HasValue)
            return null;

        var titleEditing = await titleRepository.Query(tracking: false)
            .Include(title => title.TitleCategories)
            .FirstOrDefaultAsync(title => title.Id == editingId);

        if (titleEditing == null)
            validation.AddError(TitleCreateOrUpdateErrorCode.TitleNotFound);

        return titleEditing;
    }

    private Task ValidateBasicFields<T>(
        TitleInput input,
        ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode> validation)
    {
        if (string.IsNullOrWhiteSpace(input.Description))
            validation.AddError(TitleCreateOrUpdateErrorCode.DescriptionIsRequired);
        else if (input.Description.Length > 100)
            validation.AddError(TitleCreateOrUpdateErrorCode.DescriptionTooLong);

        if (input.Value <= 0)
            validation.AddError(TitleCreateOrUpdateErrorCode.ValueMustBeGraterThanZero);

        return Task.CompletedTask;
    }

    private async Task ValidateWallet<T>(
        TitleInput input,
        ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode> validation)
    {
        var wallet = await walletRepository.Query(tracking: false)
            .FirstOrDefaultAsync(t => t.Id == input.WalletId);

        if (wallet == null)
        {
            validation.AddError(TitleCreateOrUpdateErrorCode.WalletNotFound);
            return;
        }

        if (wallet.Inactivated)
            validation.AddError(TitleCreateOrUpdateErrorCode.WalletInactive);

        if (wallet.CreatedAt > input.Date)
            validation.AddError(TitleCreateOrUpdateErrorCode.TitleDateMustBeEqualOrAfterWalletCreation);
    }

    private async Task ValidateDuplicateTitle<T>(
        TitleInput input,
        Guid? editingId,
        ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode> validation)
    {
        var duplicateExists = await titleRepository.Query(tracking: false)
            .Where(t => t.Description == input.Description.Trim()
                        && t.WalletId == input.WalletId
                        && t.Date.Year == input.Date.Year
                        && t.Date.Month == input.Date.Month
                        && t.Date.Day == input.Date.Day
                        && t.Date.Hour == input.Date.Hour
                        && t.Date.Minute == input.Date.Minute
                        && (!editingId.HasValue || t.Id != editingId.Value))
            .AnyAsync();

        if (duplicateExists)
            validation.AddError(TitleCreateOrUpdateErrorCode.DuplicateTitleInSameDateTimeMinute);
    }

    private async Task ValidateCategories<T>(
        TitleInput input,
        Title? titleEditing,
        ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode> validation)
    {
        var categories = await categoryRepository.Query(tracking: false)
            .Where(category => input.TitleCategoriesIds.Contains(category.Id))
            .ToListAsync();

        ValidateCategoriesExistence(input, categories, validation);
        if (!validation.Success) return;

        ValidateCategoriesStatus(titleEditing, categories, validation);
        if (!validation.Success) return;

        ValidateCategoriesCompatibility(input, categories, validation);
    }

    private void ValidateCategoriesExistence<T>(
        TitleInput input,
        List<TitleCategory> categories,
        ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode> validation)
    {
        var foundCategoriesIds = categories.Select(category => category.Id).ToList();
        var notFoundCategories = input.TitleCategoriesIds
            .Except(foundCategoriesIds)
            .ToList();

        if (notFoundCategories.Any())
            validation.AddError(TitleCreateOrUpdateErrorCode.SomeCategoriesNotFound, notFoundCategories);
    }

    private void ValidateCategoriesStatus<T>(
        Title? titleEditing,
        List<TitleCategory> categories,
        ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode> validation)
    {
        var previousCategoriesIds = titleEditing?.TitleCategories
            .Select(tc => tc.Id)
            .ToList() ?? new List<Guid>();

        var inactiveCategoriesIds = categories
            .Where(category => category.Inactivated
                               && !previousCategoriesIds.Contains(category.Id))
            .Select(category => category.Id)
            .ToList();

        if (inactiveCategoriesIds.Any())
            validation.AddError(TitleCreateOrUpdateErrorCode.SomeCategoriesInactive, inactiveCategoriesIds);
    }

    private void ValidateCategoriesCompatibility<T>(
        TitleInput input,
        List<TitleCategory> categories,
        ValidationResultDto<T, List<Guid>, TitleCreateOrUpdateErrorCode> validation)
    {
        var incompatibleCategories = categories
            .Where(category => !category.Type.IsCompatible(input.Type))
            .Select(c => c.Id)
            .ToList();

        if (incompatibleCategories.Any())
            validation.AddError(
                TitleCreateOrUpdateErrorCode.SomeCategoriesHasIncompatibleTypes,
                incompatibleCategories
            );
    }
}