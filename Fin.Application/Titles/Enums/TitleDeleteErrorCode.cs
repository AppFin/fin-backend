using Fin.Infrastructure.Errors;

namespace Fin.Application.Titles.Enums;

public enum TitleDeleteErrorCode
{
    [ErrorMessage("Title not found")]
    TitleNotFound = 0,
}
