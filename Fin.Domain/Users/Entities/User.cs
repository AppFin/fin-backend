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
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();

    public User()
    {
    }

    public User(UserUpdateOrCreateDto userUpdateOrCreateDto, DateTime now)
    {
        Id = Guid.NewGuid();
        
        FirstName = userUpdateOrCreateDto.FirstName;
        LastName = userUpdateOrCreateDto.LastName;
        DisplayName = userUpdateOrCreateDto.DisplayName;
        Sex = userUpdateOrCreateDto.Sex;
        BirthDate = userUpdateOrCreateDto.BirthDate;
        
        IsActivity = true;
        CreatedAt = now;
        UpdatedAt = now;
    }
    
    public void Update(UserUpdateOrCreateDto userUpdateOrCreateDto, DateTime now)
    {
        FirstName = userUpdateOrCreateDto.FirstName;
        LastName = userUpdateOrCreateDto.LastName;
        DisplayName = userUpdateOrCreateDto.DisplayName;
        Sex = userUpdateOrCreateDto.Sex;
        BirthDate = userUpdateOrCreateDto.BirthDate;
        
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