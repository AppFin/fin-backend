using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Entities;

namespace Fin.Application.Notifications.Extensions;

public static class RememberExtensions
{
    public static Notification ToNotification(this UserRememberUseSetting rememberUseSetting, DateTime today)
    {
        return new Notification(new NotificationInput
        {
            Ways = rememberUseSetting.Ways,
            StartToDelivery = today.Add(rememberUseSetting.NotifyOn),
            UserIds = [rememberUseSetting.UserId],
            Title = "Lembre-se de adicionar seus gastos",
            HtmlBody = "<span>Olá! Lembre-se de adicionar o seus gastos de hoje ao FIN.</span>",
            TextBody = "Olá! Lembre-se de adicionar o seus gastos de hoje ao FIN.",
        });
    }
}