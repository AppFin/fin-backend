using Fin.Domain.Global.Decorators;

namespace Fin.Domain.Notifications.Enums;

public enum NotificationSeverity
{
    [FrontTranslateKey("finCore.features.notifications.severity.default")]
    Default = 0,
    [FrontTranslateKey("finCore.features.notifications.severity.success")]
    Success = 1,
    [FrontTranslateKey("finCore.features.notifications.severity.error")]
    Error = 2,
    [FrontTranslateKey("finCore.features.notifications.severity.warning")]
    Warning = 3,
    [FrontTranslateKey("finCore.features.notifications.severity.info")]
    Info = 4,
}