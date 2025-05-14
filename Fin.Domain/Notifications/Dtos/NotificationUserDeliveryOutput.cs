using Fin.Domain.Notifications.Entities;

namespace Fin.Domain.Notifications.Dtos;

public class NotificationUserDeliveryOutput(NotificationUserDelivery input)
{
    public Guid NotificationId { get; set; } = input.NotificationId;
    public Guid UserId { get; set; } = input.UserId;
    public bool Delivery { get; set; } = input.Delivery;
    public bool Visualized { get; set; } = input.Visualized;
}