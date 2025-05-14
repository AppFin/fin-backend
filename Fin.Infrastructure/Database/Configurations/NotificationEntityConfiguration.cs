using Fin.Domain.Notifications;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Infrastructure.Database.ValueConverters;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Database.Configurations;

public static class NotificationEntityConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserNotificationSettings>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.AllowedWays)
                .HasConversion<EnumListToStringConverter<NotificationWay>>();
            n.HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId);
        });
        
        modelBuilder.Entity<UserRememberUseSetting>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.Ways)
                .HasConversion<EnumListToStringConverter<NotificationWay>>();
            n.Property(x => x.WeekDays)
                .HasConversion<EnumListToStringConverter<NotificationWay>>();
            n.HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId);
        });
    }
}