using System.ComponentModel.DataAnnotations;

namespace Fin.Application.Authentications.Dtos;

public class ResetPasswordInput
{
    [Required]
    public string ResetToken { get; set; }
    
    [MaxLength(100)]
    [Required]
    public string Password { get; set; }
    
    [MaxLength(100)]
    [Required]
    public string PasswordConfirmation { get; set; }
}