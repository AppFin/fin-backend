using Fin.Domain.Users.Enums;

namespace Fin.Domain.Users.Dtos;

public class UserUpdateAndCreateDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }

    public UserSex Sex { get; set; }
    public DateOnly BirthDate { get; set; }
}