using System.ComponentModel.DataAnnotations;
using Fin.Infrastructure.Authentications.Enums;

namespace Fin.Infrastructure.Authentications.Dtos;

public class LoginOutput
{
    public bool Success { get; set; } = false;
    public string Token { get; set; }
    public string RefreshToken { get; set; }
    public LoginErrorCode? ErrorCode { get; set; }
}