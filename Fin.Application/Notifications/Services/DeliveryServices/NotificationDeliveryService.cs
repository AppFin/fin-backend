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
using Fin.Infrastructure.EmailSenders.Dto;
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

        var allowedWaysToSend = userSettings.AllowedWays.Intersect(notifyUser.Ways).ToList();

        if (!userSettings.Enabled || !allowedWaysToSend.Any())
        {
            logger.LogWarning(
                "User {userId}, don't get notification {notificationId} on any way because notifications is disabled or any way is allowed.",
                notifyUser.UserId, notifyUser.NotificationId);
            return;
        }

        try
        {
            if (
                allowedWaysToSend.Contains(NotificationWay.Snack)
                | allowedWaysToSend.Contains(NotificationWay.Message)
                | allowedWaysToSend.Contains(NotificationWay.Push)
            ) await SendWebSocket(notifyUser);
            if (allowedWaysToSend.Contains(NotificationWay.Push))
                await SendFirebase(notifyUser, userSettings, false);
            if (allowedWaysToSend.Contains(NotificationWay.Email))
                await SendEmail(notifyUser);
            notificationDelivery.MarkAsDelivered();
        }
        catch (Exception e)
        {
            logger.LogError(
                "Error on send notification with id {notificationId} to user id: {userId}.\nError: {err}",
                notifyUser.NotificationId, notifyUser.UserId, e.ToString());
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
            .Select(n => new NotifyUserDto(n.Notification, n))
            .ToListAsync();

        userNotification = userNotification
            .Where(n => n.Ways.Any(n => n != NotificationWay.Email))
            .ToList();

        var notificationToMarkAsDelivery = userNotification
            .Select(u => u.NotificationId)
            .ToList();

        await deliveryRepository.Query()
            .Where(n => notificationToMarkAsDelivery.Contains(n.NotificationId))
            .ExecuteUpdateAsync(x => x
                .SetProperty(a => a.Delivery, true));

        if (autoSave) await deliveryRepository.SaveChangesAsync();

        return userNotification;
    }

    private async Task SendWebSocket(NotifyUserDto notifyUser)
    {
        await hubContext.Clients.User(notifyUser.UserId.ToString()).SendAsync(SEND_NOTIFICATION_ACTION, notifyUser);
    }

    private async Task SendFirebase(NotifyUserDto notify, UserNotificationSettings userSettings, bool autoSave)
    {
        if (userSettings.FirebaseTokens is not { Count: > 0 }) return;
        
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
        var userCredencial = await credencialRepository
            .AsNoTracking()
            .Include(c => c.User)
            .FirstOrDefaultAsync(n => n.UserId == notification.UserId);
        if (userCredencial == null)
            throw new Exception("User not found to send email notification.");

        var email = _cryptoHelper.Decrypt(userCredencial.EncryptedEmail);
        await emailSenderService.SendEmailAsync(new SendEmailDto
        {
            ToEmail = email,
            Subject = notification.Title,
            ToName = userCredencial.User.DisplayName,
            HtmlBody = notification.HtmlBody,
            PlainBody =  notification.TextBody,
        });
    }
}