using System.ComponentModel.DataAnnotations;
using Fin.Domain.Users.Enums;

namespace Fin.Domain.Users.Dtos;

public class UserUpdateOrCreateInput
{
    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public string FirstName { get; set; }
    public string LastName { get; set; }
    [Required]
    [MinLength(2)]
    [MaxLength(150)]
    public string DisplayName { get; set; }

    public UserGender Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
    
    public string ImagePublicUrl { get; set; }
}