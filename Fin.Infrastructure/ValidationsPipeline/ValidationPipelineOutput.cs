#nullable enable
namespace Fin.Infrastructure.ValidationsPipeline;

public class ValidationPipelineOutput<TErrorCode, TErrorData>(ValidationPipelineOutput<TErrorCode> validation)
{
    public TErrorCode? Code { get; set; } = validation.Code;
    public TErrorData? Data { get; set; } 
    
    public bool Success =>  Code == null;

    public ValidationPipelineOutput(): this(new ValidationPipelineOutput<TErrorCode>())
    {
    }

    public ValidationPipelineOutput<TErrorCode, TErrorData> AddError(TErrorCode code, TErrorData? data)
    {
        Code = code;
        Data = data;
        return this;
    }
    
    public ValidationPipelineOutput<TErrorCode, TErrorData> AddError(TErrorCode code)
    {
        Code = code;
        return this;
    }
}

public class ValidationPipelineOutput<TErrorCode>
{
    public TErrorCode? Code { get; set; } 
    public bool Success =>  Code == null; 
    
    public ValidationPipelineOutput<TErrorCode> AddError(TErrorCode code)
    {
        Code = code;
        return this;
    }
}