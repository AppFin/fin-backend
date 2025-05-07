using System.ComponentModel.DataAnnotations;

namespace Fin.Application.Authentications.Dtos;

public class RefreshTokenInput
{
    [Required]
    public string RefreshToken { get; set; }
}