using Fin.Domain.TitleCategories.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.TitleCategories;

public class TitleCategoryConfiguration : IEntityTypeConfiguration<TitleCategory>
{
    public void Configure(EntityTypeBuilder<TitleCategory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(20).IsRequired();
    }
}