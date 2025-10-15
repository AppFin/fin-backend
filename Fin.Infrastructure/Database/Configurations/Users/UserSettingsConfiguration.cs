using Fin.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Users;

public class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("UserSettings");
        
        builder.HasKey(us => us.Id);
        
        builder.Property(us => us.TenantId)
            .IsRequired();
        
        builder.Property(us => us.UserId)
            .IsRequired();
        
        builder.Property(us => us.EmailNotifications)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(us => us.PushNotifications)
            .IsRequired()
            .HasDefaultValue(true);
        
        builder.Property(us => us.CreatedBy)
            .IsRequired();
        
        builder.Property(us => us.UpdatedBy)
            .IsRequired();
        
        builder.Property(us => us.CreatedAt)
            .IsRequired();
        
        builder.Property(us => us.UpdatedAt)
            .IsRequired();
        
        builder.HasOne(us => us.User)
            .WithMany()
            .HasForeignKey(us => us.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(us => us.UserId)
            .IsUnique();
        
        builder.HasIndex(us => us.TenantId);
    }
}
