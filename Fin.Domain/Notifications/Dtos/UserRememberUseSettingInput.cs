using Fin.Domain.Notifications.Enums;

namespace Fin.Domain.Notifications.Dtos;

public class UserRememberUseSettingInput
{
    public List<NotificationWay> Ways { get; set; } = new();
    public List<DayOfWeek> WeekDays { get; set; }
    public TimeSpan NotifyOn { get; set; }
}