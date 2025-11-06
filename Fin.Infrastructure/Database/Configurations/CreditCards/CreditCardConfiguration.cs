using Fin.Domain.CreditCards.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.CreditCards;

public class CreditCardConfiguration: IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(20).IsRequired();
        
        builder.HasIndex(x => new {x.Name, x.TenantId}).IsUnique();
        
        builder
            .Property(p => p.Limit)
            .HasColumnType("numeric(19,4)")
            .HasPrecision(19, 4);
        
        builder
            .HasOne(creditCard => creditCard.FinancialInstitution)
            .WithMany(financialInstitution => financialInstitution.CreditCards)
            .HasForeignKey(creditCard => creditCard.FinancialInstitutionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        
        builder
            .HasOne(creditCard => creditCard.CardBrand)
            .WithMany(cardBrand => cardBrand.CreditCards)
            .HasForeignKey(creditCard => creditCard.CardBrandId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        
        builder
            .HasOne(creditCard => creditCard.DebitWallet)
            .WithMany(debitWallet => debitWallet.CreditCards)
            .HasForeignKey(creditCard => creditCard.DebitWalletId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}