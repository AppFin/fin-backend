using Fin.Application.Globals.Dtos;
using Fin.Application.TitleCategories.Dtos;
using Fin.Application.TitleCategories.Enums;
using Fin.Domain.Global.Classes;
using Fin.Domain.TitleCategories.Dtos;
using Fin.Domain.TitleCategories.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.TitleCategories;

public interface ITitleCategoryService
{
    public Task<TitleCategoryOutput> Get(Guid id);
    public Task<PagedOutput<TitleCategoryOutput>> GetList(TitleCategoryGetListInput input);
    public Task<ValidationResultDto<TitleCategoryOutput, TitleCategoryCreateOrUpdateErrorCode>> Create(TitleCategoryInput input, bool autoSave = false);
    public Task<ValidationResultDto<bool, TitleCategoryCreateOrUpdateErrorCode>> Update(Guid id, TitleCategoryInput input, bool autoSave = false);
    public Task<bool> Delete(Guid id, bool autoSave = false);
    public Task<bool> ToggleInactive(Guid id, bool autoSave = false);
}

public class TitleCategoryService(
    IRepository<TitleCategory> repository
    ) : ITitleCategoryService, IAutoTransient
{
    public async Task<TitleCategoryOutput> Get(Guid id)
    {
        var entity = await repository.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        return entity != null ? new TitleCategoryOutput(entity) : null;
    }

    public async Task<PagedOutput<TitleCategoryOutput>> GetList(TitleCategoryGetListInput input)
    {
        return await repository.AsNoTracking()
            .WhereIf(input.Inactivated.HasValue, n => n.Inactivated == input.Inactivated.Value)
            .WhereIf(input.Type.HasValue, n => n.Type == input.Type.Value)
            .OrderBy(m => m.Inactivated)
            .ThenBy(m => m.Name)
            .ApplyFilterAndSorter(input)
            .Select(n => new TitleCategoryOutput(n))
            .ToPagedResult(input);
    }

    public async Task<ValidationResultDto<TitleCategoryOutput, TitleCategoryCreateOrUpdateErrorCode>> Create(TitleCategoryInput input, bool autoSave = false)
    {
        var validation = await ValidateInput<TitleCategoryOutput>(input);
        if (!validation.Success) return validation;
        
        var titleCategory = new TitleCategory(input);
        await repository.AddAsync(titleCategory, autoSave);
        validation.Data = new TitleCategoryOutput(titleCategory);
        return validation;
    }

    public async Task<ValidationResultDto<bool, TitleCategoryCreateOrUpdateErrorCode>> Update(Guid id, TitleCategoryInput input, bool autoSave = false)
    {
        var validation = await ValidateInput<bool>(input, id);
        if (!validation.Success) return validation;
        
        var titleCategory = await repository.FirstAsync(u => u.Id == id);
        titleCategory.Update(input);
        await repository.UpdateAsync(titleCategory, autoSave);

        validation.Data = true;
        return validation;   
    }

    public async Task<bool> Delete(Guid id, bool autoSave = false)
    {
        var titleCategory = await repository
            .FirstOrDefaultAsync(u => u.Id == id);
        if (titleCategory == null) return false;

        await repository.DeleteAsync(titleCategory, autoSave);
        return true;
    }

    public async Task<bool> ToggleInactive(Guid id, bool autoSave = false)
    {
        var titleCategory = await repository
            .FirstOrDefaultAsync(u => u.Id == id);
        if (titleCategory == null) return false;

        titleCategory.ToggleInactivated();
        await repository.UpdateAsync(titleCategory, autoSave);
        
        return true;  
    }

    private async Task<ValidationResultDto<T,TitleCategoryCreateOrUpdateErrorCode>> ValidateInput<T>( TitleCategoryInput input, Guid? editingId = null)
    {
        var validationResult = new ValidationResultDto<T,TitleCategoryCreateOrUpdateErrorCode>();

        if (editingId.HasValue)
        {
            var titleExists = await repository
                .AnyAsync(n => n.Id == editingId.Value);
            if (!titleExists)
            {
                validationResult.ErrorCode = TitleCategoryCreateOrUpdateErrorCode.TitleCategoryNotFound;
                validationResult.Message = "Title category not found to edit.";
                return validationResult;
            }
        }
        
        if (string.IsNullOrWhiteSpace(input.Color))
        {
            validationResult.ErrorCode = TitleCategoryCreateOrUpdateErrorCode.ColorIsRequired;
            validationResult.Message = "Color is required.";
            return validationResult;
        }
        if (input.Color.Length > 20)
        {
            validationResult.ErrorCode = TitleCategoryCreateOrUpdateErrorCode.ColorTooLong;
            validationResult.Message = "Color is too long. Max 20 characters.";
            return validationResult;
        }
        
        if (string.IsNullOrWhiteSpace(input.Icon))
        {
            validationResult.ErrorCode = TitleCategoryCreateOrUpdateErrorCode.IconIsRequired;
            validationResult.Message = "Icon is required.";
            return validationResult;
        }
        if (input.Icon.Length > 20)
        {
            validationResult.ErrorCode = TitleCategoryCreateOrUpdateErrorCode.IconTooLong;
            validationResult.Message = "Icon is too long. Max 20 characters.";
            return validationResult;
        }
        
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            validationResult.ErrorCode = TitleCategoryCreateOrUpdateErrorCode.NameIsRequired;
            validationResult.Message = "Name is required.";
            return validationResult;
        }
        if (input.Name.Length > 100)
        {
            validationResult.ErrorCode = TitleCategoryCreateOrUpdateErrorCode.NameTooLong;
            validationResult.Message = "Name is too long. Max 100 characters.";
            return validationResult;
        }
        var nameAlredInUse = await repository
            .AnyAsync(n => n.Name == input.Name && (!editingId.HasValue || n.Id != editingId));
        if (nameAlredInUse)
        {
            validationResult.ErrorCode = TitleCategoryCreateOrUpdateErrorCode.NameAlreadyInUse;
            validationResult.Message = "Name is already in use.";
            return validationResult;
        }
        
        validationResult.Success = true;
        return validationResult;
    }
}