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
            .HasMaxLength(100)
            .IsRequired()
            .IsUnicode();

        builder.Property(x => x.Code)
            .HasMaxLength(15);

        builder.Property(x => x.Icon)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Color)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.Type)
            .IsRequired();
    }
}
