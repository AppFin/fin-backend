using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Database.ValueConverters;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Database.Configurations;

public static class NotificationEntityConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(n =>
        {
            n.HasKey(x => x.Id);
            n.Property(x => x.Ways)
                .HasConversion<EnumListToStringConverter<NotificationWay>>();
            n.Property(x => x.Title)
                .HasMaxLength(250);
            
            n
                .HasMany<User>()
                .WithMany()
                .UsingEntity<NotificationUserDelivery>(
                    l => l.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId),
                    r => r.HasOne(e => e.Notification).WithMany().HasForeignKey(e => e.NotificationId));
        });
        
        modelBuilder.Entity<NotificationUserDelivery>(n => 
        {
           n.HasKey(x => new { x.NotificationId, x.UserId });
        });
        
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
                .HasConversion<EnumListToStringConverter<DayOfWeek>>();
            n.HasOne(u => u.User)
                .WithMany()
                .HasForeignKey(u => u.UserId);
        });
    }
}