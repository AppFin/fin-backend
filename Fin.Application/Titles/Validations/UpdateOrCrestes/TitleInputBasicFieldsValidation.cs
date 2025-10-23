using Fin.Application.Titles.Enums;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Validations.UpdateOrCrestes;

public class TitleInputBasicFieldsValidation: IValidationRule<TitleInput, TitleCreateOrUpdateErrorCode>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>> ValidateAsync(TitleInput input, Guid? _)
    {
        var validation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>();
        
        if (string.IsNullOrWhiteSpace(input.Description))
            validation.AddError(TitleCreateOrUpdateErrorCode.DescriptionIsRequired);
        else if (input.Description.Length > 100)
            validation.AddError(TitleCreateOrUpdateErrorCode.DescriptionTooLong);

        if (input.Value <= 0)
            validation.AddError(TitleCreateOrUpdateErrorCode.ValueMustBeGraterThanZero);
        await Task.CompletedTask;

        return validation;
    }
}