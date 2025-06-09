using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Infrastructure.Database.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Notifications;

public class UserNotificationSettingsConfiguration: IEntityTypeConfiguration<UserNotificationSettings>
{
    public void Configure(EntityTypeBuilder<UserNotificationSettings> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AllowedWays)
            .HasConversion<ListToStringConverter<NotificationWay>>();
        builder.Property(x => x.FirebaseTokens)
            .HasConversion<ListToStringConverter>();
        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId);
    }
}