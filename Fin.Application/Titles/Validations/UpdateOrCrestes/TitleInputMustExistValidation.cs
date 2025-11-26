using Fin.Application.Titles.Enums;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Validations.UpdateOrCrestes;

public class TitleInputMustExistValidation(IRepository<Title> titleRepository): IValidationRule<TitleInput, TitleCreateOrUpdateErrorCode>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>> ValidateAsync(TitleInput _, Guid? editingId = null, CancellationToken cancellationToken = default)
    {
        var validation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode>();
        if (!editingId.HasValue) return validation;
        
        var title = await titleRepository.AsNoTracking().FirstOrDefaultAsync(t => t.Id == editingId, cancellationToken);
        return title == null ? validation.AddError(TitleCreateOrUpdateErrorCode.TitleNotFound) : validation;
    }
}