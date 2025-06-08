using Fin.Domain.Notifications.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Notifications;

public class NotificationUserDeliveryConfiguration: IEntityTypeConfiguration<NotificationUserDelivery>
{
    public void Configure(EntityTypeBuilder<NotificationUserDelivery> builder)
    {
        builder.HasKey(x => new { x.NotificationId, x.UserId });
        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId)
            .HasPrincipalKey(u => u.Id);;

        builder.HasOne(u => u.Notification)
            .WithMany(nd => nd.UserDeliveries)
            .HasForeignKey(u => u.NotificationId)
            .HasPrincipalKey(nd => nd.Id);
    }
}