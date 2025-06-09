using Fin.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Users;

public class UserCredentialConfiguration: IEntityTypeConfiguration<UserCredential>
{
    public void Configure(EntityTypeBuilder<UserCredential> builder)
    {
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.EncryptedEmail).IsUnique();
        builder.HasIndex(x => x.GoogleId).IsUnique();

        builder.Property(x => x.EncryptedEmail).HasMaxLength(200);
        builder.Property(x => x.EncryptedPassword).HasMaxLength(300);
        builder.Property(x => x.GoogleId).HasMaxLength(200);
        builder.Property(x => x.ResetToken).HasMaxLength(200);

        builder
            .HasOne(x => x.User)
            .WithOne(x => x.Credential)
            .HasForeignKey<UserCredential>(x => x.UserId)
            .HasPrincipalKey<User>(x => x.Id);
    }
}