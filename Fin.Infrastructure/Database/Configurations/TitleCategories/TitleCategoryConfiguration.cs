using Fin.Domain.TitleCategories.Entities;
using Fin.Domain.Titles.Entities;
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
        
        builder.HasIndex(x => new {x.Name, x.TenantId}).IsUnique();

        builder
            .HasMany(x => x.Titles)
            .WithMany(x => x.TitleCategories)
            .UsingEntity<TitleTitleCategory>(
                l => l
                    .HasOne(ttc => ttc.Title)
                    .WithMany(title => title.TitleTitleCategories)
                    .HasForeignKey(e => e.TitleId) 
                    .OnDelete(DeleteBehavior.Cascade),
                r => r
                    .HasOne(ttc => ttc.TitleCategory)
                    .WithMany(category => category.TitleTitleCategories)
                    .HasForeignKey(e => e.TitleCategoryId)
                    .OnDelete(DeleteBehavior.Cascade)
            );
    }
}