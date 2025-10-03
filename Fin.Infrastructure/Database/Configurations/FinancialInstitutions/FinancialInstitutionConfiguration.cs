using Fin.Domain.FinancialInstitutions.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.FinancialInstitutions;

public class FinancialInstitutionConfiguration : IEntityTypeConfiguration<FinancialInstitution>
{
    public void Configure(EntityTypeBuilder<FinancialInstitution> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Code)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.Icon)
            .HasMaxLength(50);

        builder.Property(x => x.Active)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(x => x.Type)
            .IsRequired()
            .HasConversion<string>();

        builder.HasIndex(x => new { x.Name, x.TenantId })
            .IsUnique();

        builder.HasIndex(x => new { x.Code, x.TenantId })
            .IsUnique();

        builder.HasIndex(x => x.Active);

        builder.HasIndex(x => x.Type);
    }
}
