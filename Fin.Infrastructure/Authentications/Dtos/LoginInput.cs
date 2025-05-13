using System.ComponentModel.DataAnnotations;

namespace Fin.Infrastructure.Authentications.Dtos;

public class LoginInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
    [Required]
    [MinLength(5)]
    public string Password { get; set; }
}