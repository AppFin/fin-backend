using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Users;

public class TenantConfiguration: IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Locale)
            .HasMaxLength(10)
            .IsRequired()
            .HasDefaultValue("pt-BR");
        
        builder.Property(x => x.Timezone)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("America/Sao_Paulo");
        
        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired()
            .HasDefaultValue("BRL");

        builder
            .HasMany(e => e.Users)
            .WithMany(e => e.Tenants)
            .UsingEntity<TenantUser>(
                l => l.HasOne<User>().WithMany().HasForeignKey(e => e.UserId),
                r => r.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId));
    }
}