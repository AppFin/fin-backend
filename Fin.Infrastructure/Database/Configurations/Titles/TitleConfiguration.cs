using Fin.Domain.Titles.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Titles;

public class TitleConfiguration: IEntityTypeConfiguration<Title>
{
    public void Configure(EntityTypeBuilder<Title> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Description).HasMaxLength(100).IsRequired();
        
        builder
            .Property(title => title.PreviousBalance)
            .HasColumnType("numeric(19,4)")
            .HasPrecision(19, 4);
        builder
            .Property(title => title.Value)
            .HasColumnType("numeric(19,4)")
            .HasPrecision(19, 4);
        
        builder
            .HasOne(title => title.Wallet)
            .WithMany(wallet => wallet.Titles)
            .HasForeignKey(title => title.WalletId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}