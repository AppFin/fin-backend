using Fin.Application.Globals.Dtos;
using Fin.Application.Titles.Dtos;
using Fin.Application.Titles.Enums;
using Fin.Application.Wallets.Services;
using Fin.Domain.Global.Classes;
using Fin.Domain.Titles.Dtos;
using Fin.Domain.Titles.Entities;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.UnitOfWorks;
using Fin.Infrastructure.ValidationsPipeline;

namespace Fin.Application.Titles.Services;

public interface ITitleService
{
    public Task<TitleOutput> Get(Guid id, CancellationToken cancellationToken = default);
    public Task<PagedOutput<TitleOutput>> GetList(TitleGetListInput input, CancellationToken cancellationToken = default);
    public Task<ValidationResultDto<TitleOutput, TitleCreateOrUpdateErrorCode>> Create(TitleInput input, bool autoSave = false, CancellationToken cancellationToken = default);
    public Task<ValidationResultDto<bool, TitleCreateOrUpdateErrorCode>> Update(Guid id, TitleInput input, bool autoSave = false, CancellationToken cancellationToken = default);
    public Task<ValidationResultDto<bool, TitleDeleteErrorCode>> Delete(Guid id, bool autoSave = false, CancellationToken cancellationToken = default);
}

public class TitleService(
    IRepository<Title> titleRepository,
    IWalletBalanceService balanceService,
    IUnitOfWork unitOfWork,
    IValidationPipelineOrchestrator validation
    ): ITitleService
{
    public Task<TitleOutput> Get(Guid id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<PagedOutput<TitleOutput>> GetList(TitleGetListInput input, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<ValidationResultDto<TitleOutput, TitleCreateOrUpdateErrorCode>> Create(TitleInput input, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var validationResult = await ValidateInput<TitleOutput>(input, null, cancellationToken);
        if (!validationResult.Success) return validationResult;

        var previousBalance = await balanceService.GetBalanceAt(input.WalletId, input.Date, cancellationToken);
        var title = new Title(input, previousBalance);

        await using var scope = await unitOfWork.BeginTransactionAsync(cancellationToken);
        await titleRepository.AddAsync(title, cancellationToken);
        await balanceService.ReprocessBalanceFrom(title, autoSave: false, cancellationToken);
        if (autoSave) await scope.CompleteAsync(cancellationToken);

        validationResult.Data = new TitleOutput(title);
        return validationResult;
    }

    public Task<ValidationResultDto<bool, TitleCreateOrUpdateErrorCode>> Update(Guid id, TitleInput input, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<ValidationResultDto<bool, TitleDeleteErrorCode>> Delete(Guid id, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    private async Task<ValidationResultDto<TSuccess, TitleCreateOrUpdateErrorCode>> ValidateInput<TSuccess>(TitleInput input, Guid? editingId = null, CancellationToken cancellationToken = default)
    {
        var validationResult = await validation.Validate<TitleInput, TitleCreateOrUpdateErrorCode>(input, editingId, cancellationToken);
        return validationResult.ToValidationResult<TSuccess, TitleCreateOrUpdateErrorCode>();
    }
}