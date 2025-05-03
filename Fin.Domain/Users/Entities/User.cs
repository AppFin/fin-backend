using Fin.Domain.Global.Interfaces;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Enums;

namespace Fin.Domain.Users.Entities;

public class User: IEntity
{
    public Guid Id { get; set; }
    
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string DisplayName { get; set; }

    public UserSex Sex { get; set; }
    public DateOnly BirthDate { get; set; }
    public string ImageIdentifier { get; private set; } 
        
    public bool Admin { get; } = false;
    public bool IsActivity { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    
    public UserCredential Credential { get; set; }
    public ICollection<Tenant> Tenants { get; set; }

    public User()
    {
    }

    public User(UserUpdateAndCreateDto userUpdateAndCreateDto, DateTime now)
    {
        Id = Guid.NewGuid();
        
        FirstName = userUpdateAndCreateDto.FirstName;
        LastName = userUpdateAndCreateDto.LastName;
        DisplayName = userUpdateAndCreateDto.DisplayName;
        Sex = userUpdateAndCreateDto.Sex;
        BirthDate = userUpdateAndCreateDto.BirthDate;
        
        CreatedAt = now;
        UpdatedAt = now;
    }
    
    public void Update(UserUpdateAndCreateDto userUpdateAndCreateDto, DateTime now)
    {
        FirstName = userUpdateAndCreateDto.FirstName;
        LastName = userUpdateAndCreateDto.LastName;
        DisplayName = userUpdateAndCreateDto.DisplayName;
        Sex = userUpdateAndCreateDto.Sex;
        BirthDate = userUpdateAndCreateDto.BirthDate;
        
        UpdatedAt = now;
    }
    
    public void ToggleActivity()
    {
        if (!IsActivity && Credential.ExceededAttempts) 
            return;
        
        IsActivity = !IsActivity;
    }
    
    public void SetImageIdentifier(string imageIdentifier)
    {
        ImageIdentifier = imageIdentifier;
    }
}