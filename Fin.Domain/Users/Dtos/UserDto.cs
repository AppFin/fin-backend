using System.Text.Json.Serialization;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Fin.Domain.Users.Enums;

namespace Fin.Domain.Users.Dtos;

public class UserDto
{
    public Guid Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }
    public UserGender Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string ImagePublicUrl { get; private set; }    
    public bool IsAdmin { get; } = false;
    public bool IsActivity { get; private set; }

    [JsonIgnore]
    public List<Tenant> Tenants { get; private set; } = [];

    public UserDto()
    {
    }
    
    public UserDto(User user)
    {
        Id = user.Id;
        FirstName = user.FirstName;
        LastName = user.LastName;
        DisplayName = user.DisplayName;
        Gender = user.Gender;
        BirthDate = user.BirthDate;
        ImagePublicUrl = user.ImagePublicUrl;
        IsAdmin = user.IsAdmin;
        IsActivity = user.IsActivity;
        Tenants = user.Tenants?.ToList();
    }
}