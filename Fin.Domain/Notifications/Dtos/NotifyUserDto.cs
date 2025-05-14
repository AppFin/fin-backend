using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;

namespace Fin.Domain.Notifications.Dtos;

public class NotifyUserDto(Notification notification, NotificationUserDelivery notificationUserDelivery)
{
    public List<NotificationWay> Ways { get; set; } = notification.Ways;
    public string TextBody { get; set; } = notification.TextBody;
    public string HtmlBody { get; set; } = notification.HtmlBody;
    public string Title { get; set; } = notification.Title;
    
    public Guid NotificationId { get; set; } = notification.Id;
    public Guid UserId { get; set; } = notificationUserDelivery.UserId;
}