using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;

namespace Fin.Domain.Notifications.Dtos;

public class NotificationOutput(Notification input)
{
    public Guid Id { get; set; } = input.Id;
    public List<NotificationWay> Ways { get; set; } = input.Ways;
    public string TextBody { get; set; } = input.TextBody;
    public string HtmlBody { get; set; } = input.HtmlBody;
    public string Title { get; set; } = input.Title;
    public bool Continuous { get; set; } = input.Continuous;
    public DateTime StartToDelivery { get; set; } = input.StartToDelivery;
    public DateTime? StopToDelivery { get; set; } = input.StopToDelivery;
    public string Link { get; set; } = input.Link;
    public NotificationSeverity Severity { get; set; } = input.Severity;
    public List<Guid> UserIds { get; set; } = input.UserDeliveries.Select(u => u.UserId).ToList();

    public NotificationOutput() : this(new Notification())
    {
    }
}