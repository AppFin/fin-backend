using Fin.Application.Globals.Dtos;
using Fin.Application.People.Dtos;
using Fin.Application.People.Enums;
using Fin.Domain.Global.Classes;
using Fin.Domain.People.Dtos;
using Fin.Domain.People.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Extensions;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.People;

public interface IPersonService
{
    public Task<PersonOutput> Get(Guid id);
    public Task<PagedOutput<PersonOutput>> GetList(PersonGetListInput input);
    public Task<ValidationResultDto<PersonOutput, PersonCreateOrUpdateErrorCode>> Create(PersonInput input, bool autoSave = false);
    public Task<ValidationResultDto<bool, PersonCreateOrUpdateErrorCode>> Update(Guid id, PersonInput input, bool autoSave = false);
    public Task<ValidationResultDto<bool, PersonDeleteErrorCode>> Delete(Guid id, bool autoSave = false);
    public Task<bool> ToggleInactive(Guid id, bool autoSave = false);
}

public class PersonService(
    IRepository<Person> repository
    ) : IPersonService, IAutoTransient
{
    public async Task<PersonOutput> Get(Guid id)
    {
        var entity = await repository.AsNoTracking().FirstOrDefaultAsync(n => n.Id == id);
        return entity != null ? new PersonOutput(entity) : null;
    }

    public async Task<PagedOutput<PersonOutput>> GetList(PersonGetListInput input)
    {
        return await repository
            .AsNoTracking()
            .WhereIf(input.Inactivated.HasValue, n => n.Inactivated == input.Inactivated.Value)
            .OrderBy(m => m.Inactivated)
            .ThenBy(m => m.Name)
            .ApplyFilterAndSorter(input)
            .Select(n => new PersonOutput(n))
            .ToPagedResult(input);
    }

    public async Task<ValidationResultDto<PersonOutput, PersonCreateOrUpdateErrorCode>> Create(PersonInput input, bool autoSave = false)
    {
        var validation = await ValidateInput<PersonOutput>(input);
        if (!validation.Success) return validation;
        
        var person = new Person(input);
        await repository.AddAsync(person, autoSave);
        validation.Data = new PersonOutput(person);
        return validation;
    }

    public async Task<ValidationResultDto<bool, PersonCreateOrUpdateErrorCode>> Update(Guid id, PersonInput input, bool autoSave = false)
    {
        var validation = await ValidateInput<bool>(input, id);
        if (!validation.Success) return validation;
        
        var person = await repository.FirstAsync(u => u.Id == id);
        person.Update(input);
        await repository.UpdateAsync(person, autoSave);

        validation.Data = true;
        return validation;   
    }

    public async Task<ValidationResultDto<bool, PersonDeleteErrorCode>> Delete(Guid id, bool autoSave = false)
    {
        var validation = new ValidationResultDto<bool, PersonDeleteErrorCode>();
        
        var person = await repository
            .Include(u => u.TitlePeople)
            .FirstOrDefaultAsync(u => u.Id == id);
        if (person == null) return validation.WithError(PersonDeleteErrorCode.PersonNotFound);
        if (person.TitlePeople != null && person.TitlePeople.Any()) validation.WithError(PersonDeleteErrorCode.PersonInUse);

        await repository.DeleteAsync(person, autoSave);
        return validation.WithSuccess(true);
    }

    public async Task<bool> ToggleInactive(Guid id, bool autoSave = false)
    {
        var person = await repository
            .FirstOrDefaultAsync(u => u.Id == id);
        if (person == null) return false;

        person.ToggleInactivated();
        await repository.UpdateAsync(person, autoSave);
        
        return true;  
    }

    private async Task<ValidationResultDto<T,PersonCreateOrUpdateErrorCode>> ValidateInput<T>( PersonInput input, Guid? editingId = null)
    {
        var validationResult = new ValidationResultDto<T,PersonCreateOrUpdateErrorCode>();

        if (editingId.HasValue)
        {
            var titleExists = await repository.AnyAsync(n => n.Id == editingId.Value);
            if (!titleExists)
                return validationResult.WithError(PersonCreateOrUpdateErrorCode.PersonNotFound);
        }
        
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            return validationResult.WithError(PersonCreateOrUpdateErrorCode.NameIsRequired);
        }
        if (input.Name.Length > 100)
        {
            return validationResult.WithError(PersonCreateOrUpdateErrorCode.NameTooLong);
        }
        var nameAlredInUse = await repository
            .AnyAsync(n => n.Name == input.Name && (!editingId.HasValue || n.Id != editingId));
        if (nameAlredInUse)
        {
            validationResult.ErrorCode = PersonCreateOrUpdateErrorCode.NameAlreadyInUse;
            validationResult.Message = "Name is already in use.";
            return validationResult;
        }
        
        validationResult.Success = true;
        return validationResult;
    }
}