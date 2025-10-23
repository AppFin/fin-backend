#nullable enable
using Fin.Infrastructure.Errors;

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
    
    public ValidationResultDto<TDSuccess, TDError, TErroCode> AddError(TErroCode errorCode, TDError errorData, string? message = null)
    {
        ErrorCode = errorCode;
        ErrorData = errorData;
        InternalMessage = message;
        return this;
    }
}

public class ValidationResultDto<TDSuccess, TErroCode> : ValidationResultDto<TDSuccess, object, TErroCode> where TErroCode : Enum
{
    public new ValidationResultDto<TDSuccess, TErroCode> AddError(TErroCode errorCode, string? message = null)
    {
        ErrorCode = errorCode;
        InternalMessage = message;
        return this;
    }
}

public class ValidationResultDto<TDSuccess> : ValidationResultDto<TDSuccess, object, Enum>
{
}
