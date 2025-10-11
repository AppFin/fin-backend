using Fin.Domain.Wallets.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.Wallets;

public class WalletConfiguration: IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(20).IsRequired();
        
        builder.HasIndex(x => new {x.Name, x.TenantId}).IsUnique();
        
        builder
            .Property(p => p.InitialBalance)
            .HasColumnType("numeric(19,4)")
            .HasPrecision(19, 4);
        
        builder
            .HasOne(wallet => wallet.FinancialInstitution)
            .WithMany(financialInstitution => financialInstitution.Wallets)
            .HasForeignKey(wallet => wallet.FinancialInstitutionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);
    }
}