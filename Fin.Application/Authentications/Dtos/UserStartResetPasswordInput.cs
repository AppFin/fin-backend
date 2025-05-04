using System.ComponentModel.DataAnnotations;

namespace Fin.Application.Authentications.Dtos;

public class UserStartResetPasswordInput
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}