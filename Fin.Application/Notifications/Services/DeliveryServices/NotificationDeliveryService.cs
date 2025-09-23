using Fin.Domain.Global;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Authentications.Constants;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.EmailSenders;
using Fin.Infrastructure.Firebases;
using Fin.Infrastructure.Notifications.Hubs;
using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    IDateTimeProvider dateTimeProvider,
    IHubContext<NotificationHub> hubContext,
    IEmailSenderService emailSenderService,
    IFirebaseNotificationService firebaseNotification,
    ILogger<NotificationDeliveryService> logger)
    : INotificationDeliveryService, IAutoTransient
{
    private readonly string SEND_NOTIFICATION_ACTION = "ReceiveNotification";

    private readonly CryptoHelper _cryptoHelper = new(
        configuration.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value ?? "",
        configuration.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value ?? ""
    );


    public async Task SendNotification(NotifyUserDto notifyUser, bool autoSave = true)
    {
        var notificationDelivery = await deliveryRepository.Query()
            .FirstOrDefaultAsync(n => n.NotificationId == notifyUser.NotificationId && n.UserId == notifyUser.UserId);
        if (notificationDelivery == null)
            throw new Exception(
                $"Notification not found to send. NotificationId {notifyUser.NotificationId}, UserId {notifyUser.UserId}");

        var userSettings = await userSettingsRepository.Query()
            .FirstOrDefaultAsync(u => u.UserId == notifyUser.UserId);
        if (userSettings == null)
            throw new Exception(
                $"User Notification Settings not found to send. NotificationId {notifyUser.NotificationId}, UserId {notifyUser.UserId}");

        foreach (var way in notifyUser.Ways)
        {
            if (!userSettings.Enabled || !userSettings.AllowedWays.Contains(way))
            {
                logger.LogInformation(
                    "User {userId}, don't recived notfication {notificationId} on way {way} because notifications is disabled or way is not allowed.",
                    notifyUser.UserId, notifyUser.NotificationId, way);
                continue;
            }

            try
            {
                switch (way)
                {
                    case NotificationWay.Snack:
                    case NotificationWay.Message:
                        await SendWebSocket(notifyUser);
                        break;
                    case NotificationWay.Push:
                        await SendWebSocket(notifyUser);
                        await SendFirebase(notifyUser, userSettings, false);
                        break;
                    case NotificationWay.Email:
                        await SendEmail(notifyUser);
                        if (notifyUser.Ways.Count == 1)
                            notificationDelivery.MarkAsDelivered();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception e)
            {
                logger.LogError(
                    "Error on send notification with id {notificationId} to user id: {userID} on way: {way}. Error: {err}",
                    notifyUser.NotificationId, notifyUser.UserId, way, e);
            }
        }

        await deliveryRepository.UpdateAsync(notificationDelivery);

        if (autoSave)
            await deliveryRepository.SaveChangesAsync();
    }

    public async Task<bool> MarkAsVisualized(Guid notificationId, bool autoSave = true)
    {
        if (!ambientData.IsLogged)
            throw new UnauthorizedAccessException("User not logged");

        var userId = ambientData.UserId.Value;
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
        if (!ambientData.IsLogged)
            throw new UnauthorizedAccessException("User not logged");

        var userId = ambientData.UserId.Value;
        var now = dateTimeProvider.UtcNow();

        var userNotification = await deliveryRepository.Query(tracking: false)
            .Include(u => u.Notification)
            .Where(n => !n.Visualized && n.UserId == userId)
            .Where(n => n.Notification.StartToDelivery <= now.AddMinutes(1))
            .Where(n => !n.Notification.StopToDelivery.HasValue ||
                        n.Notification.StopToDelivery.Value >= now)
            .Where(n => n.Notification.Ways.Any(n => n != NotificationWay.Email))
            .Select(n => new NotifyUserDto(n.Notification, n))
            .ToListAsync();

        var notificationToMarkAsRead = userNotification
            .Where(n => n.Ways.Contains(NotificationWay.Push))
            .Select(u => u.NotificationId)
            .ToList();

        await deliveryRepository.Query()
            .Where(n => notificationToMarkAsRead.Contains(n.NotificationId))
            .ExecuteUpdateAsync(x => x
                .SetProperty(a => a.Visualized, true));

        if (autoSave) await deliveryRepository.SaveChangesAsync();

        return userNotification;
    }

    private async Task SendWebSocket(NotifyUserDto notifyUser)
    {
        await hubContext.Clients.User(notifyUser.UserId.ToString()).SendAsync(SEND_NOTIFICATION_ACTION, notifyUser);
    }
    
    private async Task SendFirebase(NotifyUserDto notify, UserNotificationSettings userSettings, bool autoSave)
    {
        var messages = userSettings.FirebaseTokens
            .Select(t => new Message
            {
                Data = new Dictionary<string, string>
                {
                    { "title", notify.Title },
                    { "htmlBody", notify.HtmlBody },
                    { "textBody", notify.TextBody },
                    { "notificationId", notify.NotificationId.ToString() },
                    { "severity", notify.Severity.ToString() },
                    { "link", notify.Link },
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
            throw new Exception("User not found to send email notification.");

        var email = _cryptoHelper.Decrypt(userCredencial.EncryptedEmail);
        await emailSenderService.SendEmailAsync(email, notification.Title, notification.HtmlBody);
    }
}