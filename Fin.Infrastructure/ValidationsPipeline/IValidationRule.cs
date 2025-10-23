namespace Fin.Infrastructure.ValidationsPipeline;

public interface IValidationRule<TInput, TErrorCode, TErrorData>
{
    public Task<ValidationPipelineOutput<TErrorCode, TErrorData>> ValidateAsync(TInput input, Guid? editingId = null);
}

public interface IValidationRule<TInput, TErrorCode>
{
    public Task<ValidationPipelineOutput<TErrorCode>> ValidateAsync(TInput titleId, Guid? editingId = null);
    
}