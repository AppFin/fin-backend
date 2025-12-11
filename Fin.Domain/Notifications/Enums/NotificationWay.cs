using Fin.Domain.Global.Decorators;

namespace Fin.Domain.Notifications.Enums;

public enum NotificationWay
{
    [FrontTranslateKey("finCore.features.notifications.ways.snack")]
    Snack = 0,
    [FrontTranslateKey("finCore.features.notifications.ways.message")]
    Message = 1,
    [FrontTranslateKey("finCore.features.notifications.ways.push")]
    Push = 2,
    [FrontTranslateKey("finCore.features.notifications.ways.email")]
    Email = 3
}