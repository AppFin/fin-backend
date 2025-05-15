using Fin.Domain.Global.Interfaces;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Users.Entities;

namespace Fin.Domain.Notifications.Entities;

public class UserRememberUseSetting : IAuditedTenantEntity
{
    public Guid UserId { get; set; }
    public List<NotificationWay> Ways { get; set; } = new();
    public TimeSpan NotifyOn { get; set; }
    public List<DayOfWeek> WeekDays { get; set; }

    public virtual User User { get; set; }

    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid TenantId { get; set; }

    public UserRememberUseSetting()
    {
    }

    public UserRememberUseSetting(Guid userId)
    {
        Id = Guid.NewGuid();
        UserId = userId;
    }

    public void Update(UserRememberUseSettingInput input)
    {
        Ways = input.Ways;
        WeekDays = input.WeekDays;
        WeekDays = input.WeekDays;
    }
}