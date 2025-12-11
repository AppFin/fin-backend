using Fin.Domain.Global.Interfaces;
using Fin.Domain.Users.Entities;

namespace Fin.Domain.Notifications.Entities;

public class NotificationUserDelivery: ILoggable
{
    public Guid NotificationId { get; set; }
    public Guid UserId { get; set; }
    public bool Delivery { get; set; }
    public bool Visualized { get; set; }

    public string BackgroundJobId { get; set; }

    public virtual Notification Notification { get; set; }
    public virtual User User { get; set; }
    
    public NotificationUserDelivery()
    {
    }

    public NotificationUserDelivery(Guid userId, Guid notificationId)
    {
        UserId = userId;
        NotificationId = notificationId;
    }
    
    public void MarkAsDelivered()
    {
        Delivery = true;
    }
    
    public void MarkAsVisualized()
    {
        Visualized = true;
    }

    public object GetLog()
    {
        return new
        {
            NotificationId,
            UserId,
            Delivery,
            Visualized,
            BackgroundJobId
        };
    }
}