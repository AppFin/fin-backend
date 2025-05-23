using Fin.Application.Notifications.Hubs;
using Fin.Domain.Global;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Authentications.Consts;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.EmailSenders;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Fin.Application.Notifications.DeliveryServices;

public interface INotificationDeliveryService
{
    public Task SendNotification(NotifyUserDto notificationDelivery, bool autoSave = true);
    public Task<bool> MarkAsVisualized(Guid notificationId, bool autoSave = true);
}

public class NotificationDeliveryService(
    IHubContext<NotificationHub> hubContext,
    IRepository<NotificationUserDelivery> deliveryRepository,
    IRepository<UserCredential> credencialRepository,
    IConfiguration configuration,
    IAmbientData _ambientData,
    IEmailSenderService emailSenderService)
    : INotificationDeliveryService, IAutoTransient
{

    private readonly CryptoHelper _cryptoHelper = new(
        configuration.GetSection(AuthenticationConsts.EncryptKeyConfigKey).Value ?? "",
        configuration.GetSection(AuthenticationConsts.EncryptIvConfigKey).Value ?? ""
        );


    public async Task SendNotification(NotifyUserDto notificationDelivery, bool autoSave = true)
    {
        var notification = await deliveryRepository.Query()
            .FirstOrDefaultAsync(n => n.NotificationId == notificationDelivery.NotificationId &&
                                      n.UserId == notificationDelivery.UserId);
        if (notification == null)
            throw new Exception($"Notification not found to send. NotificationId {notificationDelivery.NotificationId}, UserId {notificationDelivery.UserId}");


        foreach (var way in notificationDelivery.Ways)
        {
            switch (way)
            {
                case NotificationWay.Snack:
                case NotificationWay.Message:
                    await hubContext.Clients.User(notificationDelivery.UserId.ToString())
                        .SendAsync("ReceiveNotification", notificationDelivery);
                    break;
                case NotificationWay.Push:
                    // aqui vai o firebase tbm
                    await hubContext.Clients.User(notificationDelivery.UserId.ToString())
                        .SendAsync("ReceiveNotification", notificationDelivery);
                    break;
                case NotificationWay.Email:
                    await SendEmail(notificationDelivery);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        notification.MarkAsDelivered();
        await deliveryRepository.UpdateAsync(notification, autoSave);
    }

    public async Task<bool> MarkAsVisualized(Guid notificationId, bool autoSave = true)
    {
        if (!_ambientData.IsLogged)
            throw new UnauthorizedAccessException("User not logged");

        var userId = _ambientData.UserId;
        var notification = await deliveryRepository.Query()
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
        if (notification == null)
            return false;

        notification.MarkAsVisualized();
        await deliveryRepository.UpdateAsync(notification, autoSave);
        return true;
    }

    private async Task SendEmail(NotifyUserDto notification)
    {
        var userCredencial = await credencialRepository.Query(false)
            .FirstOrDefaultAsync(n => n.UserId == notification.UserId);
        if (userCredencial == null)
            throw new Exception($"User not found to send email notification. UserId {notification.UserId}, NotificationId {notification.NotificationId}");

        var email = _cryptoHelper.Decrypt(userCredencial.EncryptedEmail);
        await emailSenderService.SendEmailAsync(email, notification.Title, notification.HtmlBody);
    }
}