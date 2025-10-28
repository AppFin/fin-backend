namespace Fin.Infrastructure.ValidationsPipeline;

public interface IValidationRule<TInput, TErrorCode, TErrorData> where TErrorCode : struct
{
    public Task<ValidationPipelineOutput<TErrorCode, TErrorData>> ValidateAsync(TInput input, Guid? editingId = null, CancellationToken cancellationToken = default);
}

public interface IValidationRule<TInput, TErrorCode> where TErrorCode : struct
{
    public Task<ValidationPipelineOutput<TErrorCode>> ValidateAsync(TInput titleId, Guid? editingId = null, CancellationToken cancellationToken = default);
    
}