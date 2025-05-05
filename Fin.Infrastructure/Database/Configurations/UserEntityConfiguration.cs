using Fin.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Database.Configurations;

public static class UserEntityConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(u =>
        {
            u.HasKey(x => x.Id);
            
            u.Property(x => x.FirstName).HasMaxLength(100);
            u.Property(x => x.LastName).HasMaxLength(100);
            u.Property(x => x.DisplayName).HasMaxLength(150);
            u.Property(x => x.ImagePublicUrl).HasMaxLength(200);
        });

        modelBuilder.Entity<UserCredential>(u =>
        {
            u.HasKey(x => x.Id);
            
            u.HasIndex(x => x.EncryptedEmail).IsUnique();
            u.HasIndex(x => x.GoogleId).IsUnique();
            u.HasIndex(x => x.ResetToken).IsUnique();
            
            u.Property(x => x.EncryptedEmail).HasMaxLength(200);
            u.Property(x => x.EncryptedPassword).HasMaxLength(300);
            u.Property(x => x.GoogleId).HasMaxLength(200);
            u.Property(x => x.ResetToken).HasMaxLength(200);
            
            u
                .HasOne(x => x.User)
                .WithOne(x => x.Credential)
                .HasForeignKey<UserCredential>(x => x.UserId)
                .HasPrincipalKey<User>(x => x.Id);
        });
    }
}