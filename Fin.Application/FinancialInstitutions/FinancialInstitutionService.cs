using Fin.Domain.FinancialInstitutions.Dtos;
using Fin.Domain.FinancialInstitutions.Entities;
using Fin.Domain.Global.Classes;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.FinancialInstitutions;

public interface IFinancialInstitutionService
{
    public Task<FinancialInstitutionOutput> Get(Guid id);
    public Task<PagedOutput<FinancialInstitutionOutput>> GetList(PagedFilteredAndSortedInput input);
    public Task<FinancialInstitutionOutput> Create(FinancialInstitutionInput input, bool autoSave = false);
    public Task<bool> Update(Guid id, FinancialInstitutionInput input, bool autoSave = false);
    public Task<bool> Delete(Guid id, bool autoSave = false);
    public Task<bool> Activate(Guid id, bool autoSave = false);
    public Task<bool> Deactivate(Guid id, bool autoSave = false);
}

public class FinancialInstitutionService(
    IRepository<FinancialInstitution> repository
    ) : IFinancialInstitutionService, IAutoTransient
{
    public async Task<FinancialInstitutionOutput> Get(Guid id)
    {
        var entity = await repository.Query()
            .FirstOrDefaultAsync(n => n.Id == id);
        return entity != null ? new FinancialInstitutionOutput(entity) : null;
    }

    public async Task<PagedOutput<FinancialInstitutionOutput>> GetList(PagedFilteredAndSortedInput input)
    {
        return await repository.Query(false)
            .OrderBy(f => f.Name)
            .ApplyFilterAndSorter(input)
            .Select(n => new FinancialInstitutionOutput(n))
            .ToPagedResult(input);
    }

    public async Task<FinancialInstitutionOutput> Create(FinancialInstitutionInput input, bool autoSave = false)
    {
        ValidateInput(input);
        
        var existingName = await repository.Query()
            .AnyAsync(f => f.Name == input.Name);
        if (existingName)
            throw new InvalidOperationException("A financial institution with this name already exists.");

        var existingCode = await repository.Query()
            .AnyAsync(f => f.Code == input.Code);
        if (existingCode)
            throw new InvalidOperationException("A financial institution with this code already exists.");

        var institution = new FinancialInstitution(input);
        await repository.AddAsync(institution, autoSave);
        return new FinancialInstitutionOutput(institution);
    }

    public async Task<bool> Update(Guid id, FinancialInstitutionInput input, bool autoSave = false)
    {
        ValidateInput(input);
        
        var institution = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (institution == null) return false;

        var existingName = await repository.Query()
            .AnyAsync(f => f.Name == input.Name && f.Id != id);
        if (existingName)
            throw new InvalidOperationException("A financial institution with this name already exists.");

        var existingCode = await repository.Query()
            .AnyAsync(f => f.Code == input.Code && f.Id != id);
        if (existingCode)
            throw new InvalidOperationException("A financial institution with this code already exists.");

        institution.Update(input);
        await repository.UpdateAsync(institution, autoSave);
        
        return true;   
    }

    public async Task<bool> Delete(Guid id, bool autoSave = false)
    {
        var institution = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (institution == null) return false;

        await repository.DeleteAsync(institution, autoSave);
        return true;
    }

    public async Task<bool> Activate(Guid id, bool autoSave = false)
    {
        var institution = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (institution == null) return false;

        institution.Activate();
        await repository.UpdateAsync(institution, autoSave);
        return true;
    }

    public async Task<bool> Deactivate(Guid id, bool autoSave = false)
    {
        var institution = await repository.Query()
            .FirstOrDefaultAsync(u => u.Id == id);
        if (institution == null) return false;

        institution.Deactivate();
        await repository.UpdateAsync(institution, autoSave);
        return true;
    }

    private void ValidateInput(FinancialInstitutionInput input)
    {
        if (input == null)
            throw new ArgumentNullException(nameof(input));

        if (string.IsNullOrWhiteSpace(input.Name))
            throw new ArgumentException("Name is required.");

        if (input.Name?.Length > 200)
            throw new ArgumentException("Name must not exceed 200 characters.");

        if (string.IsNullOrWhiteSpace(input.Code))
            throw new ArgumentException("Code is required.");

        if (!System.Text.RegularExpressions.Regex.IsMatch(input.Code ?? "", @"^\d{3}$"))
            throw new ArgumentException("Code must be exactly 3 digits.");
    }
}
