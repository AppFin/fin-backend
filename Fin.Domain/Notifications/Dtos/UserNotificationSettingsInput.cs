using Fin.Domain.Notifications.Enums;

namespace Fin.Domain.Notifications.Dtos;

public class UserNotificationSettingsInput
{
    public bool Enabled { get; set; }
    public List<NotificationWay> AllowedWays { get; set; }
}