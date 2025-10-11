using Fin.Application.CardBrands;
using Fin.Domain.Global.Classes;
using Fin.Domain.CardBrands.Dtos;
using Fin.Domain.CardBrands.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.CardBrands;

public interface ICardBrandService
{
    public Task<CardBrandOutput> Get(Guid id);
    public Task<PagedOutput<CardBrandOutput>> GetList(PagedFilteredAndSortedInput input);
    public Task<CardBrandOutput> Create(CardBrandInput input, bool autoSave = false);
    public Task<bool> Update(Guid id, CardBrandInput input, bool autoSave = false);
    public Task<bool> Delete(Guid id, bool autoSave = false);
    public Task<List<CardBrandOutput>> GetListForSideNav();
}

public class CardBrandService(
    IRepository<CardBrand> repository,
    IAmbientData ambientData
    ) : ICardBrandService, IAutoTransient
{
    public async Task<CardBrandOutput> Get(Guid id)
    {
        var entity = await repository.Query()
            .FirstOrDefaultAsync(n => n.Id == id);
        return entity != null ? new CardBrandOutput(entity) : null;
    }

    public async Task<PagedOutput<CardBrandOutput>> GetList(PagedFilteredAndSortedInput input)
    {
        return await repository.Query(false)
            .OrderBy(m => m.Name)
            .ApplyFilterAndSorter(input)
            .Select(n => new CardBrandOutput(n))
            .ToPagedResult(input);
    }

    public async Task<List<CardBrandOutput>> GetListForSideNav()
    {
        return await repository.Query(false)
            .OrderBy(m => m.Name)
            .Select(m => new CardBrandOutput(m))
            .ToListAsync();
    }

    public async Task<CardBrandOutput> Create(CardBrandInput input, bool autoSave = false)
    {
        ValidarInput(input);
        var cardBrand = new CardBrand(input);
        await repository.AddAsync(cardBrand, autoSave);
        return new CardBrandOutput(cardBrand);
    }

    public async Task<bool> Update(Guid id, CardBrandInput input, bool autoSave = false)
    {
        ValidarInput(input);
        var cardBrand = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (cardBrand == null) return false;

        cardBrand.Update(input);
        await repository.UpdateAsync(cardBrand, autoSave);
        
        return true;   
    }

    public async Task<bool> Delete(Guid id, bool autoSave = false)
    {
        var cardBrand = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (cardBrand == null) return false;

        await repository.DeleteAsync(cardBrand, autoSave);
        return true;
    }

    private static void ValidarInput(CardBrandInput input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new BadHttpRequestException("Name is required");
        if (string.IsNullOrWhiteSpace(input.Icon))
            throw new BadHttpRequestException("Icon is required");
    }
}