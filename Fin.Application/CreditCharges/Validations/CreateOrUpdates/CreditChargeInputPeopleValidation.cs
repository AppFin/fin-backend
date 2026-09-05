using Fin.Application.CreditCharges.Dtos;
using Fin.Application.CreditCharges.Enums;
using Fin.Domain.People.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.CreditCharges.Validations.CreateOrUpdates;

public class CreditChargeInputPeopleValidation(IRepository<Person> personRepository): IValidationRule<CreditChargeInput, CreditChargeCreateOrUpdateErrorCode>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<CreditChargeCreateOrUpdateErrorCode>> ValidateAsync(CreditChargeInput input, Guid? _, CancellationToken cancellationToken = default)
    {
        var validation = new ValidationPipelineOutput<CreditChargeCreateOrUpdateErrorCode>();
        
        if (!input.CreditChargePeople.Any())
            return validation;

        var personIds = input.CreditChargePeople.Select(p => p.PersonId).ToList();
        var people = await personRepository.AsNoTracking()
            .Where(p => personIds.Contains(p.Id))
            .ToListAsync(cancellationToken);

        if (people.Count != personIds.Count)
            validation.AddError(CreditChargeCreateOrUpdateErrorCode.SomePeopleNotFound);
        
        if (people.Any(p => p.Inactivated))
            validation.AddError(CreditChargeCreateOrUpdateErrorCode.SomePeopleInactive);
        
        if (input.CreditChargePeople.Any(p => p.Percentage < 0 || p.Percentage > 100))
            validation.AddError(CreditChargeCreateOrUpdateErrorCode.PeopleSplitRange);

        return validation;
    }
}

