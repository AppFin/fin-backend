namespace Fin.Application.Titles.Enums;

public enum TitleDeleteErrorCode
{
    TitleNotFound = 1,
}

public static class TitleDeleteErrorMessages
{
    const string TitleNotFound = "Title not found";

    public static string GetMessage(this TitleDeleteErrorCode errorCode)
    {
        return errorCode switch
        {
            TitleDeleteErrorCode.TitleNotFound => TitleNotFound,
            _ => throw new ArgumentOutOfRangeException(nameof(errorCode), errorCode, null)
        };
    }
}