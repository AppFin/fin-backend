using System.Collections.ObjectModel;
using Fin.Domain.Global.Extensions;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.Notifications.Dtos;
using Fin.Domain.Notifications.Enums;

namespace Fin.Domain.Notifications.Entities;

public class Notification: IAuditedEntity
{
    public List<NotificationWay> Ways { get; set; } = [];
    public string TextBody { get; set; }
    public string HtmlBody { get; set; }
    public string NormalizedTextBody { get; set; }
    public string Title { get; set; }
    public bool Continuous { get; set; }
    public string NormalizedTitle { get; set; }
    public DateTime StartToDelivery { get; set; }
    public DateTime? StopToDelivery { get; set; }
    public string Link { get; set; }
    public NotificationSeverity Severity { get; set; }
    
    public Guid Id { get; set; }
    public Guid CreatedBy { get; set; }
    public Guid UpdatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Collection<NotificationUserDelivery> UserDeliveries { get; set; } = [];

    public Notification()
    {
    }

    public Notification(NotificationInput input)
    {
        Id = Guid.NewGuid();
        
        Ways = input.Ways;
        TextBody = input.TextBody;
        HtmlBody = input.HtmlBody;
        NormalizedTextBody = TextBody.NormalizeForComparison();
        Continuous = input.Continuous;

        Title = input.Title;
        Link = input.Link;
        Severity = input.Severity;
        NormalizedTitle = Title.NormalizeForComparison();
        StartToDelivery = input.StartToDelivery;
        StopToDelivery = input.StopToDelivery;
        
        UserDeliveries = new Collection<NotificationUserDelivery>(
            input.UserIds.Select(userId => new NotificationUserDelivery(userId, Id)).ToList()
        );
    }
    
    public List<NotificationUserDelivery> UpdateAndReturnToRemoveDeliveries(NotificationInput input)
    {
        Ways = input.Ways;
        TextBody = input.TextBody;
        HtmlBody = input.HtmlBody;
        Title = input.Title;
        StartToDelivery = input.StartToDelivery;
        StopToDelivery = input.StopToDelivery;
        Continuous = input.Continuous;
        Link = input.Link;
        Severity = input.Severity;

        var updatedDeliveries = input.UserIds.Select(userId => new NotificationUserDelivery(userId, Id)).ToList();
        
        var deliveriesToDelete = new List<NotificationUserDelivery>();
        foreach (var currentDelivery in UserDeliveries)
        {
            var index = updatedDeliveries.FindIndex(c => c.UserId == currentDelivery.UserId);
            if (index != -1) continue;
            deliveriesToDelete.Add(currentDelivery);
        }

        foreach (var currentDelivery in deliveriesToDelete)
        {
            UserDeliveries.Remove(currentDelivery);
        }

        foreach (var updatedDelivery in updatedDeliveries)
        {
            var index = UserDeliveries.ToList().FindIndex(c => c.UserId == updatedDelivery.UserId);
            if (index != -1) continue;
            UserDeliveries.Add(updatedDelivery);
        }

        return deliveriesToDelete;
    }
}