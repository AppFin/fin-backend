using Fin.Domain.Global.Interfaces;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Users.Entities;

namespace Fin.Domain.Notifications.Entities;

public class UserNotificationSettings: IAuditedTenantEntity
{
    public Guid UserId { get; set; }
    public bool Enabled { get; set; }
    public List<NotificationWay> AllowedWays { get; set; }
    public List<string> FirebaseTokens { get; set; }

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
    
    public UserNotificationSettings(Guid userId, Guid tenantId)
    {
        Id = Guid.NewGuid();
        
        TenantId = tenantId;
        CreatedBy = userId;
        UpdatedBy = userId;
        
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

    public void Update(UserNotificationSettingsInput input)
    {
        Enabled = input.Enabled;
        AllowedWays = input.AllowedWays;
    }

    public bool AddTokenIfNotExist(string token)
    {
        if (FirebaseTokens.Contains(token)) return false;
        FirebaseTokens.Add(token);
        return true;
    }

    public void RemoveTokens(List<string> tokens)
    {
        FirebaseTokens = FirebaseTokens.Except(tokens).ToList();
    }
}