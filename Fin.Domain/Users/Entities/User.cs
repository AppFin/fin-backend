using System.Text.Json.Serialization;
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

    public UserGender Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
    public string ImagePublicUrl { get; private set; } 
    public string Theme { get; set; } = "light";

    public bool IsAdmin { get; private set; } = false;
    public bool IsActivity { get; private set; }
    
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public UserCredential Credential { get; set; }
    
    [JsonIgnore]
    public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();

    public virtual ICollection<UserDeleteRequest> DeleteRequests { get; set; } = new List<UserDeleteRequest>();

    public User()
    {
    }

    public User(UserUpdateOrCreateInput userUpdateOrCreateInput, DateTime now)
    {
        Id = Guid.NewGuid();
        
        FirstName = userUpdateOrCreateInput.FirstName;
        LastName = userUpdateOrCreateInput.LastName;
        DisplayName = userUpdateOrCreateInput.DisplayName;
        Gender = userUpdateOrCreateInput.Gender;
        BirthDate = userUpdateOrCreateInput.BirthDate;
        ImagePublicUrl = userUpdateOrCreateInput.ImagePublicUrl;
        
        IsActivity = true;
        CreatedAt = now;
        UpdatedAt = now;
    }
    
    public void Update(UserUpdateOrCreateInput userUpdateOrCreateInput, DateTime now)
    {
        FirstName = userUpdateOrCreateInput.FirstName;
        LastName = userUpdateOrCreateInput.LastName;
        DisplayName = userUpdateOrCreateInput.DisplayName;
        Gender = userUpdateOrCreateInput.Gender;
        BirthDate = userUpdateOrCreateInput.BirthDate;
        ImagePublicUrl = userUpdateOrCreateInput.ImagePublicUrl;
        
        UpdatedAt = now;
    }

    public UserDeleteRequest RequestDelete(DateTime deleteRequestAt)
    {
        IsActivity = false;
        var deleteRequest = new  UserDeleteRequest(Id, deleteRequestAt);

        DeleteRequests ??= new List<UserDeleteRequest>();
        DeleteRequests.Add(deleteRequest);

        return deleteRequest;
    }
    
    public void ToggleActivity()
    {
        if (!IsActivity && Credential.ExceededAttempts) 
            return;
        
        IsActivity = !IsActivity;
    }
    
    public void SetImageIdentifier(string imageIdentifier)
    {
        ImagePublicUrl = imageIdentifier;
    }
    
    public void UpdateTheme(string theme, DateTime now)
    {
        if (!string.IsNullOrWhiteSpace(theme))
        {
            var validThemes = new[] { "light", "dark", "auto" };
            if (!validThemes.Contains(theme.ToLower()))
                throw new ArgumentException("Theme must be one of: light, dark, auto");
            
            Theme = theme.ToLower();
        }
        UpdatedAt = now;
    }

    public void MakeAdmin()
    {
        IsAdmin = true;
    }
}