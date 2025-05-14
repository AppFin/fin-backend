using Fin.Domain.Global.Interfaces;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Users.Entities;

namespace Fin.Domain.Notifications.Entities;

public class UserNotificationSettings: IAuditedTenantEntity
{
    public Guid UserId { get; set; }
    public bool Enabled { get; set; }
    public List<NotificationWay> AllowedWays { get; set; }
    
    public virtual User User { get; set; }

    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }

    public UserNotificationSettings()
    {
    }
    
    public UserNotificationSettings(Guid userId)
    {
        Id = Guid.NewGuid();
        
        UserId = userId;
        Enabled = true;
        AllowedWays = new List<NotificationWay>
        {
            NotificationWay.Snack,
            NotificationWay.Message,
            NotificationWay.Push,
            NotificationWay.Email
        };
    }

    public void Update(UserNotificationSettings input)
    {
        Enabled = input.Enabled;
        AllowedWays = input.AllowedWays;
    }
}