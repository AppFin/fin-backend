using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;

namespace Fin.Domain.Notifications.Dtos;

public class UserNotificationSettingsOutput(UserNotificationSettings input)
{
    public Guid Id { get; set; } = input.Id;
    public Guid UserId { get; set; } = input.UserId;
    public bool Enabled { get; set; } = input.Enabled;
    public List<NotificationWay> AllowedWays { get; set; } = input.AllowedWays;

    public UserNotificationSettingsOutput():this(new UserNotificationSettings())
    {
    }
}