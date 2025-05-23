using Fin.Application.Notifications.Hubs;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.SignalR;

namespace Fin.Application.Notifications.DeliveryServices;

public interface INotificationDeliveryService
{
    public Task SendNotification(NotifyUserDto notification, bool markAsSend = true);
}

public class NotificationDeliveryService(
    IHubContext<NotificationHub> hubContext,
    IRepository<NotificationUserDelivery> repository)
    : INotificationDeliveryService, IAutoTransient
{

    public Task SendNotification(NotifyUserDto notification, bool markAsSend = true)
    {
        foreach (var way in notification.Ways)
        {
            switch (way)
            {
                case NotificationWay.Snack:
                case NotificationWay.Message:
                case NotificationWay.Push:
                case NotificationWay.Email:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}