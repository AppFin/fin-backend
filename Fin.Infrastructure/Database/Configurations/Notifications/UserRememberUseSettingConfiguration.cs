using Fin.Domain.Notifications.Entities;
using Fin.Domain.Notifications.Enums;
using Fin.Infrastructure.Database.ValueConverters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Notifications;

public class UserRememberUseSettingConfiguration: IEntityTypeConfiguration<UserRememberUseSetting>
{
    public void Configure(EntityTypeBuilder<UserRememberUseSetting> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Ways)
            .HasConversion<ListToStringConverter<NotificationWay>>();
        builder.Property(x => x.WeekDays)
            .HasConversion<ListToStringConverter<DayOfWeek>>();
        builder.HasOne(u => u.User)
            .WithMany()
            .HasForeignKey(u => u.UserId);
    }
}