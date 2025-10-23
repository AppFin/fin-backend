using Fin.Application.Titles.Enums;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Domain.Wallets.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.ValidationsPipeline;
using Microsoft.EntityFrameworkCore;

namespace Fin.Application.Titles.Validations.UpdateOrCrestes;

public class TitleInputWalletValidation(IRepository<Wallet> walletRepository): IValidationRule<TitleInput, TitleCreateOrUpdateErrorCode, List<Guid>>, IAutoTransient
{
    public async Task<ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>>> ValidateAsync(TitleInput input, Guid? _)
    {
        var validation = new ValidationPipelineOutput<TitleCreateOrUpdateErrorCode, List<Guid>>();
        
        var wallet = await walletRepository.Query(tracking: false)
            .FirstOrDefaultAsync(t => t.Id == input.WalletId);

        if (wallet == null)
        {
            validation.AddError(TitleCreateOrUpdateErrorCode.WalletNotFound);
            return validation;
        }

        if (wallet.Inactivated)
            validation.AddError(TitleCreateOrUpdateErrorCode.WalletInactive);

        if (wallet.CreatedAt > input.Date)
            validation.AddError(TitleCreateOrUpdateErrorCode.TitleDateMustBeEqualOrAfterWalletCreation);
        
        return validation;
    }
}