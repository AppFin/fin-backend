using Fin.Infrastructure.AutoServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.ValidationsPipeline;

public interface IValidationPipelineOrchestrator
{
    Task<ValidationPipelineOutput<TErrorCode, TErrorData>> Validate<TInput, TErrorCode, TErrorData>(TInput input, Guid? editingId = null, CancellationToken cancellationToken = default) where TErrorCode: struct;
    
    Task<ValidationPipelineOutput<TErrorCode>> Validate<TInput, TErrorCode>(TInput input, Guid? editingId = null, CancellationToken cancellationToken = default) where TErrorCode: struct;
}

public class ValidationPipelineOrchestrator(IServiceProvider serviceProvider): IValidationPipelineOrchestrator, IAutoTransient
{
    public async Task<ValidationPipelineOutput<TErrorCode, TErrorData>> Validate<TInput, TErrorCode, TErrorData>(TInput input, Guid? editingId = null, CancellationToken cancellationToken = default) where TErrorCode: struct
    {
        var rulesWithOutData = serviceProvider.GetServices<IValidationRule<TInput, TErrorCode>>();
        foreach (var rule in rulesWithOutData)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var validation = await rule.ValidateAsync(input, editingId, cancellationToken);
            if (!validation.Success) return new ValidationPipelineOutput<TErrorCode, TErrorData>(validation);
        }
        
        var rules = serviceProvider.GetServices<IValidationRule<TInput, TErrorCode, TErrorData>>();
        foreach (var rule in rules)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var validation = await rule.ValidateAsync(input, editingId, cancellationToken);
            if (!validation.Success) return validation;
        }
        
        return new ValidationPipelineOutput<TErrorCode, TErrorData>();
    }

    public async Task<ValidationPipelineOutput<TErrorCode>> Validate<TInput, TErrorCode>(TInput input, Guid? editingId = null, CancellationToken cancellationToken = default) where TErrorCode: struct
    {
        var rules = serviceProvider.GetServices<IValidationRule<TInput, TErrorCode>>();
        foreach (var rule in rules)
        {
            if (cancellationToken.IsCancellationRequested) break;
            var validation = await rule.ValidateAsync(input, editingId, cancellationToken);
            if (!validation.Success) return validation;
        }
        return new ValidationPipelineOutput<TErrorCode>();
    }
}