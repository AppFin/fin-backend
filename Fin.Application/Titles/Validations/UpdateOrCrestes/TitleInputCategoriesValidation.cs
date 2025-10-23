using Fin.Application.Titles.Enums;
using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.TitleCategories.Enums;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Validations.UpdateOrCrestes;

public class TitleInputCategoriesValidation(
    IRepository<Title> titleRepository,
    IRepository<TitleCategory> categoryRepository
    ): IValidationRule<TitleInput, TitleCreateOrUpdateErrorCode, List<Guid>>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>>> ValidateAsync(TitleInput input, Guid? editingId = null)
    {
        var validation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>>();
        
        var categories = await categoryRepository.Query(tracking: false)
            .Where(category => input.TitleCategoriesIds.Contains(category.Id))
            .ToListAsync();

        ValidateCategoriesExistence(input, categories, validation);
        if (!validation.Success) return validation;

        var titleEditing = !editingId.HasValue ? null : await titleRepository.Query(tracking: false)
            .FirstOrDefaultAsync(title => title.Id == editingId.Value);
        ValidateCategoriesStatus(titleEditing, categories, validation);
        if (!validation.Success) return validation;

        ValidateCategoriesCompatibility(input, categories, validation);

        return validation;
    }
    
    private void ValidateCategoriesExistence(
        TitleInput input,
        List<TitleCategory> categories,
        ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>> validation)
    {
        var foundCategoriesIds = categories.Select(category => category.Id).ToList();
        var notFoundCategories = input.TitleCategoriesIds
            .Except(foundCategoriesIds)
            .ToList();

        if (notFoundCategories.Any())
            validation.AddError(TitleCreateOrUpdateErrorCode.SomeCategoriesNotFound, notFoundCategories);
    }

    private void ValidateCategoriesStatus(
        Title? titleEditing,
        List<TitleCategory> categories,
        ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>> validation)
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

    private void ValidateCategoriesCompatibility(
        TitleInput input,
        List<TitleCategory> categories,
        ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>> validation)
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