using Fin.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Users;

public class UserDeleteRequestConfiguration: IEntityTypeConfiguration<UserDeleteRequest>
{
    public void Configure(EntityTypeBuilder<UserDeleteRequest> builder)
    {
        builder.HasKey(x => x.Id);

        builder
            .HasOne(e => e.User)
            .WithOne(e => e.DeleteRequest)
            .HasForeignKey<UserDeleteRequest>(x => x.UserId)
            .HasPrincipalKey<User>(x => x.Id);

        builder
            .HasOne(e => e.UserAborted)
            .WithMany()
            .HasForeignKey(x => x.UserAbortedId)
            .HasPrincipalKey(x => x.Id);
    }
}