using Fin.Domain.Notifications.Enums;

namespace Fin.Domain.Notifications.Dtos;

public class NotificationInput
{
    public List<NotificationWay> Ways { get; set; } = new();
    public string TextBody { get; set; }
    public string HtmlBody { get; set; }
    public string Title { get; set; }
    public bool Continuous { get; set; }
    public DateTime StartToDelivery { get; set; }
    public DateTime? StopToDelivery { get; set; }
    public List<Guid> UserIds { get; set; } = new();
    public string Link { get; set; }
    public NotificationSeverity Severity { get; set; }
}