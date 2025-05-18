using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Infrastructure.AutoServices.Interfaces;

namespace Fin.Application.Notifications.DeliveryServices;

public interface INotificationDeliveryService
{
    public Task SendNotification(NotifyUserDto notification);
}

public class NotificationDeliveryService: INotificationDeliveryService, IAutoTransient
{
    public Task SendNotification(NotifyUserDto notification)
    {
        throw new NotImplementedException();
    }
}