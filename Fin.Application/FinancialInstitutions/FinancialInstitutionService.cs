using Fin.Domain.FinancialInstitutions.Dtos;
using Fin.Domain.FinancialInstitutions.Entities;
using Fin.Domain.Global.Classes;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.FinancialInstitutions;

public interface IFinancialInstitutionService
{
    Task<FinancialInstitutionOutput> Get(Guid id);
    Task<PagedOutput<FinancialInstitutionOutput>> GetList(FinancialInstitutionGetListInput input);
    Task<FinancialInstitutionOutput> Create(FinancialInstitutionInput input, bool autoSave = false);
    Task<bool> Update(Guid id, FinancialInstitutionInput input, bool autoSave = false);
    Task<bool> Delete(Guid id, bool autoSave = false);
    Task<bool> ToggleInactive(Guid id, bool autoSave = false);
}

public class FinancialInstitutionService(
    IRepository<FinancialInstitution> repository
    ) : IFinancialInstitutionService, IAutoTransient
{
    public async Task<FinancialInstitutionOutput> Get(Guid id)
    {
        var entity = await repository.Query(false)
            .FirstOrDefaultAsync(f => f.Id == id);
        return entity != null ? new FinancialInstitutionOutput(entity) : null;
    }

    public async Task<PagedOutput<FinancialInstitutionOutput>> GetList(FinancialInstitutionGetListInput input)
    {
        return await repository.Query(false)
            .WhereIf(input.Inactive.HasValue, f => f.Inactive == input.Inactive.Value)
            .WhereIf(input.Type.HasValue, f => f.Type == input.Type.Value)
            .OrderBy(f => f.Inactive)
            .ThenBy(f => f.Name)
            .ApplyFilterAndSorter(input)
            .Select(f => new FinancialInstitutionOutput(f))
            .ToPagedResult(input);
    }

    public async Task<FinancialInstitutionOutput> Create(FinancialInstitutionInput input, bool autoSave = false)
    {
        await ValidateInput(input);

        var institution = new FinancialInstitution(input);
        await repository.AddAsync(institution, autoSave);
        return new FinancialInstitutionOutput(institution);
    }

    public async Task<bool> Update(Guid id, FinancialInstitutionInput input, bool autoSave = false)
    {
        await ValidateInput(input, id);
        var institution = await repository.Query()
            .FirstOrDefaultAsync(f => f.Id == id);
        if (institution == null) return false;

      

        institution.Update(input);
        await repository.UpdateAsync(institution, autoSave);

        return true;
    }

    public async Task<bool> Delete(Guid id, bool autoSave = false)
    {
        var institution = await repository.Query()
            .Include(f => f.Wallets)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (institution == null || institution.Wallets.Any()) return false;
        
        await repository.DeleteAsync(institution, autoSave);
        return true;
    }

    public async Task<bool> ToggleInactive(Guid id, bool autoSave = false)
    {
        var institution = await repository.Query()
            .Include(f => f.Wallets)
            .FirstOrDefaultAsync(f => f.Id == id);
        if (institution == null || (!institution.Inactive && institution.Wallets.Any(w => !w.Inactivated))) return false;

        institution.ToggleInactive();

        await repository.UpdateAsync(institution, autoSave);
        return true;
    }

    private async Task ValidateInput(FinancialInstitutionInput input, Guid? editingId = null)
    {

        if (string.IsNullOrWhiteSpace(input.Name))
            throw new BadHttpRequestException("Name is required");
        if (input.Name.Count() > 100)
            throw new BadHttpRequestException("Name must be at most 100 characters long");
        if (string.IsNullOrWhiteSpace(input.Icon))
            throw new BadHttpRequestException("Icon is required");
        if (input.Icon.Count() > 20)
            throw new BadHttpRequestException("Icon must be at most 20 characters long");

        if (string.IsNullOrWhiteSpace(input.Color))
            throw new BadHttpRequestException("Color is required");
        if (input.Color.Count() > 20)
            throw new BadHttpRequestException("Color must be at most 20 characters long");

        if(!string.IsNullOrWhiteSpace(input.Code) && input.Code.Count() > 15)
            throw new BadHttpRequestException("Code must be at most 15 characters long");

        var existingName = await repository.Query()
            .Where(f => f.Name == input.Name)
            .WhereIf(editingId.HasValue, f => f.Id != editingId.Value)
            .AnyAsync();

        if (existingName)
            throw new BadHttpRequestException("A financial institution with this name already exists");
    }
}
