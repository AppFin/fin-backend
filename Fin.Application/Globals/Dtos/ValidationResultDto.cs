#nullable enable
using Fin.Infrastructure.Errors;
using Fin.Infrastructure.ValidationsPipeline;

namespace Fin.Application.Globals.Dtos;

public class ValidationResultDto<TDSuccess, TDError, TErroCode> where TErroCode : Enum
{
    public TDSuccess? Data { get; set; }
    public TDError? ErrorData { get; set; }
    public TErroCode? ErrorCode { get; set; }

    public bool Success
    {
        get
        {
            if (InternalSuccess.HasValue) return InternalSuccess.Value;
            return ErrorCode == null;
        }
        set => InternalSuccess = value;
    }

    public string Message
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(InternalMessage))
            {
                return InternalMessage;
            }

            if (Success) return "Success";

            return ErrorCode != null ? ErrorCode.GetErrorMessage() : string.Empty;
        }
        set => InternalMessage = value;
    }

    protected bool? InternalSuccess { get; set; }
    protected string? InternalMessage { get; set; }

    public ValidationResultDto<TDSuccess, TDError, TErroCode> AddError(TErroCode errorCode, string? message = null)
    {
        ErrorCode = errorCode;
        InternalMessage = message;
        return this;
    }

    public ValidationResultDto<TDSuccess, TDError, TErroCode> AddError(TErroCode errorCode, TDError errorData,
        string? message = null)
    {
        ErrorCode = errorCode;
        ErrorData = errorData;
        InternalMessage = message;
        return this;
    }

    public static ValidationResultDto<TDSuccess, TDError, TErroCode> FromPipeline(
        ValidationPipelineOutput<TErroCode, TDError> pipelineOutput)
    {
        return new ValidationResultDto<TDSuccess, TDError, TErroCode>
        {
            ErrorData = pipelineOutput.Data,
            ErrorCode = pipelineOutput.Code,
        };
    }
}

public class ValidationResultDto<TDSuccess, TErroCode> : ValidationResultDto<TDSuccess, object, TErroCode>
    where TErroCode : Enum
{
    public new ValidationResultDto<TDSuccess, TErroCode> AddError(TErroCode errorCode, string? message = null)
    {
        ErrorCode = errorCode;
        InternalMessage = message;
        return this;
    }

    public static ValidationResultDto<TDSuccess, TErroCode> FromPipeline(
        ValidationPipelineOutput<TErroCode> pipelineOutput)
    {
        return new ValidationResultDto<TDSuccess, TErroCode>
        {
            ErrorCode = pipelineOutput.Code,
        };
    }
}

public class ValidationResultDto<TDSuccess> : ValidationResultDto<TDSuccess, object, Enum>
{
}

public static class ValidationResultDtoExtensions
{
    public static ValidationResultDto<TSuccess, TError, TErrorCode> ToValidationResult<TSuccess, TError, TErrorCode>(
        this ValidationPipelineOutput<TErrorCode, TError> pipeline)
        where TErrorCode : Enum
    {
        return ValidationResultDto<TSuccess, TError, TErrorCode>.FromPipeline(pipeline);
    }

    public static ValidationResultDto<TSuccess, TErrorCode> ToValidationResult<TSuccess, TErrorCode>(
        this ValidationPipelineOutput<TErrorCode> pipeline)
        where TErrorCode : Enum
    {
        return ValidationResultDto<TSuccess, TErrorCode>.FromPipeline(pipeline);
    }
}