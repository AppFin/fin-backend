using Fin.Application.CreditCharges.Dtos;
using Fin.Application.CreditCharges.Enums;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.ValidationsPipeline;

namespace Fin.Application.CreditCharges.Validations.CreateOrUpdates;

public class CreditChargeInputBasicFieldsValidation: IValidationRule<CreditChargeInput, CreditChargeCreateOrUpdateErrorCode>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<CreditChargeCreateOrUpdateErrorCode>> ValidateAsync(CreditChargeInput input, Guid? _, CancellationToken __ = default)
    {
        var validation = new ValidationPipelineOutput<CreditChargeCreateOrUpdateErrorCode>();
        
        if (string.IsNullOrWhiteSpace(input.Description))
            validation.AddError(CreditChargeCreateOrUpdateErrorCode.DescriptionIsRequired);
        else if (input.Description.Length > 100)
            validation.AddError(CreditChargeCreateOrUpdateErrorCode.DescriptionTooLong);

        if (input.Value <= 0)
            validation.AddError(CreditChargeCreateOrUpdateErrorCode.ValueMustBeGreaterThanZero);
        
        if (input.NumberOfInstallments < 1)
            validation.AddError(CreditChargeCreateOrUpdateErrorCode.NumberOfInstallmentsMustBePositive);
        
        await Task.CompletedTask;

        return validation;
    }
}

