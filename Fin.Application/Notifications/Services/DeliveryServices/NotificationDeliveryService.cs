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
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.Firebases;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Notification = FirebaseAdmin.Messaging.Notification;

namespace Fin.Application.Notifications.Services.DeliveryServices;

public interface INotificationDeliveryService
{
    public Task SendNotification(NotifyUserDto notifyUser, bool autoSave = true);
    public Task<bool> MarkAsVisualized(Guid notificationId, bool autoSave = true);
    public Task<List<NotifyUserDto>> GetUnvisualizedNotifications(bool autoSave = true);
}

public class NotificationDeliveryService(
    IRepository<NotificationUserDelivery> deliveryRepository,
    IRepository<UserCredential> credencialRepository,
    IRepository<UserNotificationSettings> userSettingsRepository,
    IConfiguration configuration,
    IAmbientData ambientData,
    IDateTimeProvider _dateTimeProvider,

    IHubContext<NotificationHub> hubContext,
    IEmailSenderService emailSenderService,
    IFirebaseNotificationService firebaseNotification
    )
    : INotificationDeliveryService, IAutoTransient
{
    private readonly string SEND_NOTIFICATION_ACTION = "ReceiveNotification";
    private readonly CryptoHelper _cryptoHelper = new(
        configuration.GetSection(AuthenticationConsts.EncryptKeyConfigKey).Value ?? "",
        configuration.GetSection(AuthenticationConsts.EncryptIvConfigKey).Value ?? ""
        );


    public async Task SendNotification(NotifyUserDto notifyUser, bool autoSave = true)
    {
        var notificationDelivery = await deliveryRepository.Query()
            .FirstOrDefaultAsync(n => n.NotificationId == notifyUser.NotificationId && n.UserId == notifyUser.UserId);
        if (notificationDelivery == null)
            throw new Exception($"Notification not found to send. NotificationId {notifyUser.NotificationId}, UserId {notifyUser.UserId}");

        var userSettings = await userSettingsRepository.Query()
            .FirstOrDefaultAsync(u => u.UserId == notifyUser.UserId);
        if (userSettings == null)
            throw new Exception($"User Notification Settings not found to send. NotificationId {notifyUser.NotificationId}, UserId {notifyUser.UserId}");

        foreach (var way in notifyUser.Ways)
        {
            if (way is NotificationWay.Snack or NotificationWay.Message)
            {
                await hubContext.Clients.User(notifyUser.UserId.ToString()).SendAsync(SEND_NOTIFICATION_ACTION, notifyUser);
                continue;
            }

            if (!userSettings.Enabled || !userSettings.AllowedWays.Contains(way)) continue;

            if (way is NotificationWay.Push)
                await SendPush(notifyUser, userSettings, false);
            if (way is NotificationWay.Email)
                    await SendEmail(notifyUser);
        }

        notificationDelivery.MarkAsDelivered();
        await deliveryRepository.UpdateAsync(notificationDelivery, false);

        if (autoSave)
            await deliveryRepository.SaveChangesAsync();
    }

    public async Task<bool> MarkAsVisualized(Guid notificationId, bool autoSave = true)
    {
        if (!ambientData.IsLogged)
            throw new UnauthorizedAccessException("User not logged");

        var userId = ambientData.UserId;
        var notification = await deliveryRepository.Query()
            .FirstOrDefaultAsync(n => n.NotificationId == notificationId && n.UserId == userId);
        if (notification == null)
            return false;

        notification.MarkAsVisualized();
        await deliveryRepository.UpdateAsync(notification, autoSave);
        return true;
    }

    public async Task<List<NotifyUserDto>> GetUnvisualizedNotifications(bool autoSave = true)
    {
        var userId = ambientData.UserId;
        var now = _dateTimeProvider.UtcNow();

        var notifications = await deliveryRepository.Query(tracking: false)
            .Include(u => u.Notification)
            .Where(n => !n.Visualized && n.UserId == userId)
            .Where(n => n.Notification.StartToDelivery <= now)
            .Where(n => !n.Notification.StopToDelivery.HasValue ||
                        n.Notification.StopToDelivery.Value >= now)
            .ToListAsync();
        var userNotification = notifications
            .Where(n => n.Notification.Ways.Contains(NotificationWay.Push) ||
                        n.Notification.Ways.Contains(NotificationWay.Message) ||
                        n.Notification.Ways.Contains(NotificationWay.Snack))
            .Select(n => new NotifyUserDto(n.Notification, n))
            .ToList();

        await deliveryRepository.Query()
            .Where(n => userNotification.Select(u => u.NotificationId).Contains(n.NotificationId))
            .ExecuteUpdateAsync(x => x
                .SetProperty(a => a.Visualized, true));

        if (autoSave) await deliveryRepository.SaveChangesAsync();

        return userNotification;
    }

    private async Task SendPush(NotifyUserDto notify, UserNotificationSettings userSettings, bool autoSave)
    {
        await hubContext.Clients.User(userSettings.UserId.ToString()).SendAsync(SEND_NOTIFICATION_ACTION, notify);

        var messages = userSettings.FirebaseTokens
            .Select(t => new Message
            {
                Data = new Dictionary<string, string>
                {
                    { "title", notify.Title },
                    { "htmlBody", notify.HtmlBody },
                    { "textBody", notify.TextBody },
                    { "notificationId", notify.NotificationId.ToString() },
                },
                Notification = new Notification
                {
                    Title = notify.Title,
                    Body = notify.TextBody
                },
                Token = t
            })
            .ToList();

        if (!messages.Any()) return;

        var tokensToRemove = await firebaseNotification.SendPushNotificationAsync(messages);
        if (tokensToRemove.Any())
        {
            userSettings.RemoveTokens(tokensToRemove);
            await userSettingsRepository.UpdateAsync(userSettings, autoSave);
        }
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