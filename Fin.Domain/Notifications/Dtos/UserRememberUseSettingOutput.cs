using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;

namespace Fin.Domain.Notifications.Dtos;

public class UserRememberUseSettingOutput(UserRememberUseSetting input)
{
    public Guid Id { get; set; } = input.Id;
    public Guid UserId { get; set; } = input.UserId;
    public List<NotificationWay> Ways { get; set; } = input.Ways;
    public TimeSpan NotifyOn { get; set; } = input.NotifyOn;
}