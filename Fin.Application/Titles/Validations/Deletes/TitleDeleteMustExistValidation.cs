using Fin.Application.Titles.Enums;
using Fin.Domain.Titles.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Validations.Deletes;

public class TitleDeleteMustExistValidation(IRepository<Title> titleRepository): IValidationRule<Guid, TitleDeleteErrorCode>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<TitleDeleteErrorCode>> ValidateAsync(Guid titleId, Guid? _, CancellationToken cancellationToken = default)
    {
        var validation = new ValidationPipelineOutput<TitleDeleteErrorCode>();
        var title = await titleRepository.AsNoTracking().FirstOrDefaultAsync(t => t.Id == titleId, cancellationToken);
        return title == null ? validation.AddError(TitleDeleteErrorCode.TitleNotFound) : validation;
    }
}