using System.ComponentModel.DataAnnotations;

namespace Fin.Application.Authentications.Dtos;

public class SendResetPasswordEmailInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}