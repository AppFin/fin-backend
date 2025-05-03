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
        });

        modelBuilder.Entity<UserCredential>(u =>
        {
            u.HasKey(x => x.Id);
            
            u.HasIndex(x => x.EncryptedEmail).IsUnique();
            u.HasIndex(x => x.EncryptedPhone).IsUnique();
            u.HasIndex(x => x.GoogleId).IsUnique();
            u.HasIndex(x => x.TelegramChatId).IsUnique();
            u.HasIndex(x => x.ResetToken).IsUnique();
            
            u
                .HasOne(x => x.User)
                .WithOne(x => x.Credential)
                .HasForeignKey<UserCredential>(x => x.UserId)
                .HasPrincipalKey<User>(x => x.Id);
        });
    }
}