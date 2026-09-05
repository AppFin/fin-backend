using Fin.Application.CreditCharges.Dtos;
using Fin.Application.CreditCharges.Enums;
using Fin.Domain.CreditCharges.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.CreditCharges.Validations.CreateOrUpdates;

public class CreditChargeInputMustExistValidation(IRepository<CreditCharge> chargeRepository): IValidationRule<CreditChargeInput, CreditChargeCreateOrUpdateErrorCode>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<CreditChargeCreateOrUpdateErrorCode>> ValidateAsync(CreditChargeInput input, Guid? chargeId, CancellationToken cancellationToken = default)
    {
        var validation = new ValidationPipelineOutput<CreditChargeCreateOrUpdateErrorCode>();
        
        if (!chargeId.HasValue)
            return validation;

        var charge = await chargeRepository.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chargeId, cancellationToken);
        if (charge == null)
            validation.AddError(CreditChargeCreateOrUpdateErrorCode.CreditChargeNotFound);

        return validation;
    }
}

