namespace Fin.Application.Authentications.Enums;

public enum ResetPasswordErrorCode
{
    InvalidPassword = 1,
    NotSamePassword = 2,
    InvalidToken = 3,
    ExpiredToken = 4
}