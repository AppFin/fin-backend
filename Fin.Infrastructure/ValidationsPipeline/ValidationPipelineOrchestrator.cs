using Fin.Infrastructure.AutoServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.ValidationsPipeline;

public interface IValidationPipelineOrchestrator
{
    public Task<ValidationPipelineOutput<TErrorCode, TErrorData>> Validate<TInput, TErrorCode, TErrorData>(TInput input, Guid? editingId = null);
    public Task<ValidationPipelineOutput<TErrorCode>> Validate<TInput, TErrorCode>(TInput input, Guid? editingId = null);
}

public class ValidationPipelineOrchestrator(IServiceProvider serviceProvider): IValidationPipelineOrchestrator, IAutoTransient
{
    public async Task<ValidationPipelineOutput<TErrorCode, TErrorData>> Validate<TInput, TErrorCode, TErrorData>(TInput input, Guid? editingId = null)
    {
        var rulesWithOutData = serviceProvider.GetServices<IValidationRule<TInput, TErrorCode>>();
        foreach (var rule in rulesWithOutData)
        {
            var validation = await rule.ValidateAsync(input, editingId);
            if (!validation.Success) return new ValidationPipelineOutput<TErrorCode, TErrorData>(validation);
        }
        
        var rules = serviceProvider.GetServices<IValidationRule<TInput, TErrorCode, TErrorData>>();
        foreach (var rule in rules)
        {
            var validation = await rule.ValidateAsync(input, editingId);
            if (!validation.Success) return validation;
        }
        
        return new ValidationPipelineOutput<TErrorCode, TErrorData>();
    }

    public async Task<ValidationPipelineOutput<TErrorCode>> Validate<TInput, TErrorCode>(TInput input, Guid? editingId = null)
    {
        var rules = serviceProvider.GetServices<IValidationRule<TInput, TErrorCode>>();
        foreach (var rule in rules)
        {
            var validation = await rule.ValidateAsync(input, editingId);
            if (!validation.Success) return validation;
        }
        return new ValidationPipelineOutput<TErrorCode>();
    }
}