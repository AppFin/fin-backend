using Fin.Domain.CardBrands.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.CardBrands;

public class CardBrandConfiguration: IEntityTypeConfiguration<CardBrand>
{
    public void Configure(EntityTypeBuilder<CardBrand> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(20);
    }
}