using System.ComponentModel.DataAnnotations;

namespace Fin.Application.Authentications.Dtos;

public class UserRefreshTokenInput
{
    [Required]
    public string RefreshToken { get; set; }
}