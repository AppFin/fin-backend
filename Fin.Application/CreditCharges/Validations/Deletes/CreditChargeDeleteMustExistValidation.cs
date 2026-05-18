using Fin.Application.CreditCharges.Enums;
using Fin.Domain.CreditCharges.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.CreditCharges.Validations.Deletes;

public class CreditChargeDeleteMustExistValidation(IRepository<CreditCharge> chargeRepository): IValidationRule<Guid, CreditChargeDeleteErrorCode>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<CreditChargeDeleteErrorCode>> ValidateAsync(Guid chargeId, Guid? _, CancellationToken cancellationToken = default)
    {
        var validation = new ValidationPipelineOutput<CreditChargeDeleteErrorCode>();
        var charge = await chargeRepository.AsNoTracking().FirstOrDefaultAsync(c => c.Id == chargeId, cancellationToken);
        return charge == null ? validation.AddError(CreditChargeDeleteErrorCode.CreditChargeNotFound) : validation;
    }
}

