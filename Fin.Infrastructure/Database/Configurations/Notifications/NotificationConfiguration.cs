using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Database.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Notifications;

public class NotificationConfiguration: IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Ways)
            .HasConversion<ListToStringConverter<NotificationWay>>();
        builder.Property(x => x.Title)
            .HasMaxLength(250);

        builder
            .HasMany<User>()
            .WithMany()
            .UsingEntity<NotificationUserDelivery>(
                l => l.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).HasPrincipalKey(x => x.Id),
                r => r.HasOne(e => e.Notification).WithMany().HasForeignKey(e => e.NotificationId).HasPrincipalKey(x => x.Id));
    }
}