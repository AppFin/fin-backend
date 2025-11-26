using Fin.Application.Titles.Enums;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Validations.UpdateOrCrestes;

public class TitleInputDuplicatedValidation(IRepository<Title> titleRepository): IValidationRule<TitleInput, TitleCreateOrUpdateErrorCode>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>> ValidateAsync(TitleInput input, Guid? editingId = null, CancellationToken cancellationToken = default)
    {
        var validation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>();
        
        var duplicateExists = await titleRepository.AsNoTracking()
            .Where(t => t.Description == input.Description.Trim()
                        && t.WalletId == input.WalletId
                        && t.Date.Year == input.Date.Year
                        && t.Date.Month == input.Date.Month
                        && t.Date.Day == input.Date.Day
                        && t.Date.Hour == input.Date.Hour
                        && t.Date.Minute == input.Date.Minute
                        && (!editingId.HasValue || t.Id != editingId.Value))
            .AnyAsync(cancellationToken);

        if (duplicateExists)
            validation.AddError(TitleCreateOrUpdateErrorCode.DuplicateTitleInSameDateTimeMinute);

        return validation;
    }
}