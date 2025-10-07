using Fin.Application.TitleCategories.Dtos;
using Fin.Domain.Global.Classes;
using Fin.Domain.TitleCategories.Dtos;
using Fin.Domain.TitleCategories.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.TitleCategories;

public interface ITitleCategoryService
{
    public Task<TitleCategoryOutput> Get(Guid id);
    public Task<PagedOutput<TitleCategoryOutput>> GetList(TitleCategoryGetListInput input);
    public Task<TitleCategoryOutput> Create(TitleCategoryInput input, bool autoSave = false);
    public Task<bool> Update(Guid id, TitleCategoryInput input, bool autoSave = false);
    public Task<bool> Delete(Guid id, bool autoSave = false);
    public Task<bool> ToggleInactive(Guid id, bool autoSave = false);
}

public class TitleCategoryService(
    IRepository<TitleCategory> repository
    ) : ITitleCategoryService, IAutoTransient
{
    public async Task<TitleCategoryOutput> Get(Guid id)
    {
        var entity = await repository.Query(false).FirstOrDefaultAsync(n => n.Id == id);
        return entity != null ? new TitleCategoryOutput(entity) : null;
    }

    public async Task<PagedOutput<TitleCategoryOutput>> GetList(TitleCategoryGetListInput input)
    {
        return await repository.Query(false)
            .WhereIf(input.Inactivated.HasValue, n => n.Inactivated == input.Inactivated.Value)
            .WhereIf(input.Type.HasValue, n => n.Type == input.Type.Value)
            .OrderBy(m => m.Inactivated)
            .ThenBy(m => m.Name)
            .ApplyFilterAndSorter(input)
            .Select(n => new TitleCategoryOutput(n))
            .ToPagedResult(input);
    }

    public async Task<TitleCategoryOutput> Create(TitleCategoryInput input, bool autoSave = false)
    {
        ValidarInput(input);
        var titleCategory = new TitleCategory(input);
        await repository.AddAsync(titleCategory, autoSave);
        return new TitleCategoryOutput(titleCategory);
    }

    public async Task<bool> Update(Guid id, TitleCategoryInput input, bool autoSave = false)
    {
        ValidarInput(input);
        var titleCategory = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (titleCategory == null) return false;

        titleCategory.Update(input);
        await repository.UpdateAsync(titleCategory, autoSave);
        
        return true;   
    }

    public async Task<bool> Delete(Guid id, bool autoSave = false)
    {
        var titleCategory = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (titleCategory == null) return false;

        await repository.DeleteAsync(titleCategory, autoSave);
        return true;
    }

    public async Task<bool> ToggleInactive(Guid id, bool autoSave = false)
    {
        var titleCategory = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (titleCategory == null) return false;

        titleCategory.ToggleInactivated();
        await repository.UpdateAsync(titleCategory, autoSave);
        
        return true;  
    }

    private static void ValidarInput( TitleCategoryInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Color))
            throw new BadHttpRequestException("FrontRoute is required");
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new BadHttpRequestException("Name is required");
        if (string.IsNullOrWhiteSpace(input.Icon))
            throw new BadHttpRequestException("Icon is required");
    }
}