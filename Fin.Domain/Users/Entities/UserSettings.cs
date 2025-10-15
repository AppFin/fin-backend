using Fin.Domain.Global.Interfaces;

namespace Fin.Domain.Users.Entities;

public class UserSettings : ITenantEntity, IAuditedEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    
    public bool EmailNotifications { get; set; } = true;
    public bool PushNotifications { get; set; } = true;
    
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    public virtual User User { get; set; }
    
    public UserSettings()
    {
    }
    
    public UserSettings(Guid userId, bool emailNotifications = true, bool pushNotifications = true)
    {
        UserId = userId;
        EmailNotifications = emailNotifications;
        PushNotifications = pushNotifications;
    }
    
    public void Update(bool emailNotifications, bool pushNotifications)
    {
        EmailNotifications = emailNotifications;
        PushNotifications = pushNotifications;
    }
}
