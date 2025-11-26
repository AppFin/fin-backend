using Fin.Domain.Global.Classes;
using Fin.Domain.Menus.Dtos;
using Fin.Domain.Menus.Entities;
using Fin.Domain.Menus.Enums;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Menus;

public interface IMenuService
{
    public Task<MenuOutput> Get(Guid id);
    public Task<PagedOutput<MenuOutput>> GetList(PagedFilteredAndSortedInput input);
    public Task<MenuOutput> Create(MenuInput input, bool autoSave = false);
    public Task<bool> Update(Guid id, MenuInput input, bool autoSave = false);
    public Task<bool> Delete(Guid id, bool autoSave = false);
    public Task<List<MenuOutput>> GetListForSideNav();
}

public class MenuService(
    IRepository<Menu> repository,
    IAmbientData ambientData
    ) : IMenuService, IAutoTransient
{
    public async Task<MenuOutput> Get(Guid id)
    {
        var entity = await repository
            .FirstOrDefaultAsync(n => n.Id == id);
        return entity != null ? new MenuOutput(entity) : null;
    }

    public async Task<PagedOutput<MenuOutput>> GetList(PagedFilteredAndSortedInput input)
    {
        return await repository.AsNoTracking()
            .WhereIf(!ambientData.IsAdmin, m => !m.OnlyForAdmin)
            .OrderBy(m => m.Name)
            .ApplyFilterAndSorter(input)
            .Select(n => new MenuOutput(n))
            .ToPagedResult(input);
    }

    public async Task<List<MenuOutput>> GetListForSideNav()
    {
        return await repository.AsNoTracking()
            .OrderBy(m => m.Name)
            .Where(m => m.Position != MenuPosition.Hide)
            .WhereIf(!ambientData.IsAdmin, m => !m.OnlyForAdmin)
            .Select(m => new MenuOutput(m))
            .ToListAsync();
    }

    public async Task<MenuOutput> Create(MenuInput input, bool autoSave = false)
    {
        ValidateInput(input);
        var menu = new Menu(input);
        await repository.AddAsync(menu, autoSave);
        return new MenuOutput(menu);
    }

    public async Task<bool> Update(Guid id, MenuInput input, bool autoSave = false)
    {
        ValidateInput(input);
        var menu = await repository
            .FirstOrDefaultAsync(u => u.Id == id);
        if (menu == null) return false;

        menu.Update(input);
        await repository.UpdateAsync(menu, autoSave);
        
        return true;   
    }

    public async Task<bool> Delete(Guid id, bool autoSave = false)
    {
        var menu = await repository
            .FirstOrDefaultAsync(u => u.Id == id);
        if (menu == null) return false;

        await repository.DeleteAsync(menu, autoSave);
        return true;
    }

    private static void ValidateInput( MenuInput input)
    {
        if (string.IsNullOrWhiteSpace(input.FrontRoute))
            throw new BadHttpRequestException("FrontRoute is required");
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new BadHttpRequestException("Name is required");
        if (string.IsNullOrWhiteSpace(input.Icon))
            throw new BadHttpRequestException("Icon is required");
    }
}