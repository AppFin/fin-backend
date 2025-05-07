using System.ComponentModel.DataAnnotations;

namespace Fin.Application.Users.Dtos;

public class UserStartCreateInput
{
    [EmailAddress]
    public string Email { get; set; }
    
    [MaxLength(100)]
    public string Password { get; set; }
    
    [MaxLength(100)]
    public string PasswordConfirmation { get; set; }
}