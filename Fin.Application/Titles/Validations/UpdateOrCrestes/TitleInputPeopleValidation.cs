using Fin.Application.Titles.Enums;
using Fin.Domain.People.Dtos;
using Fin.Domain.People.Entities;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Validations.UpdateOrCrestes;

public class TitleInputPeopleValidation(
    IRepository<Title> titleRepository,
    IRepository<Person> personRepository
    ): IValidationRule<TitleInput, TitleCreateOrUpdateErrorCode, List<Guid>>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>>> ValidateAsync(TitleInput input, Guid? editingId = null, CancellationToken cancellationToken = default)
    {
        var validation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>>();
        
        if (!input.TitlePeople.Any()) return validation;
        
        var people = await personRepository
            .Where(person  => input.TitlePeople.Select(tp => tp.PersonId).Contains(person.Id))
            .ToListAsync(cancellationToken);

        ValidatePeopleExistence(input, people, validation);
        if (!validation.Success) return validation;

        var titleEditing = !editingId.HasValue ? null : await titleRepository
            .Include(title => title.TitlePeople)
            .FirstOrDefaultAsync(title => title.Id == editingId.Value, cancellationToken);
        ValidatePeopleStatus(titleEditing, people, validation);
        if (!validation.Success) return validation;

        ValidatePeopleSplitRange(input.TitlePeople, validation);

        return validation;
    }
    
    private void ValidatePeopleExistence(
        TitleInput input,
        List<Person> people,
        ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>> validation)
    {
        var foundPeopleIds = people.Select(person => person.Id).ToList();
        var notFoundPeople = input.TitlePeople
            .Select(tp => tp.PersonId)
            .Except(foundPeopleIds)
            .ToList();

        if (notFoundPeople.Any())
            validation.AddError(TitleCreateOrUpdateErrorCode.SomePeopleNotFound, notFoundPeople);
    }

    private void ValidatePeopleStatus(
        Title? titleEditing,
        List<Person> people,
        ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>> validation)
    {
        var previousPeopleIds = titleEditing?.TitlePeople?
            .Select(tc => tc.PersonId)?
            .ToList() ?? new List<Guid>();

        var inactivePeopleIds = people
            .Where(person => person.Inactivated
                               && !previousPeopleIds.Contains(person.Id))
            .Select(person => person.Id)
            .ToList();

        if (inactivePeopleIds.Any())
            validation.AddError(TitleCreateOrUpdateErrorCode.SomePeopleInactive, inactivePeopleIds);
    }

    private void ValidatePeopleSplitRange(
        List<TitlePersonInput> people,
        ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>> validation)
    {
        var splitSum = people.Sum(p => p.Percentage);
        if (splitSum is > 100 or < 0.01m)
            validation.AddError(TitleCreateOrUpdateErrorCode.PeopleSplitRange);
    }
}