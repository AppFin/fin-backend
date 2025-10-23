using Fin.Application.Globals.Dtos;
using Fin.Application.Titles.Enums;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Services;

public interface ITitleValidationService
{
    public Task<ValidationResultDto<bool, TitleDeleteErrorCode>> ValidateDelete(Guid titleId);
    public Task<ValidationResultDto<T, TitleCreateOrUpdateErrorCode>> ValidateInput<T>(TitleInput input, Guid? editingId = null);
}

public class TitleValidationService(IRepository<Title> titleRepository): ITitleValidationService, IAutoTransient
{
    public async Task<ValidationResultDto<bool, TitleDeleteErrorCode>> ValidateDelete(Guid titleId)
    {
        var validation = new ValidationResultDto<bool, TitleDeleteErrorCode>();
        
        var title = await titleRepository.Query(tracking: false).FirstOrDefaultAsync(t => t.Id == titleId);
        if (title == null)
        {
            validation.ErrorCode = TitleDeleteErrorCode.TitleNotFound;
            validation.Message = TitleDeleteErrorCode.TitleNotFound.GetMessage();
            return validation;
        }
        
        validation.Success = true;
        return validation;
    }

    public Task<ValidationResultDto<T, TitleCreateOrUpdateErrorCode>> ValidateInput<T>(TitleInput input, Guid? editingId = null)
    {
        throw new NotImplementedException();
    }
}