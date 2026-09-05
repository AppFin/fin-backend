using Fin.Domain.CreditCharges.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.CreditCharges;

public class CardBillingConfiguration: IEntityTypeConfiguration<CardBilling>
{
    public void Configure(EntityTypeBuilder<CardBilling> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder
            .Property(cardBilling => cardBilling.Value)
            .HasColumnType("numeric(19,4)")
            .HasPrecision(19, 4);
        
        builder
            .HasOne(cardBilling => cardBilling.CreditCard)
            .WithMany(card => card.CardBillings)
            .HasForeignKey(cardBilling => cardBilling.CreditCardId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(cardBilling => cardBilling.PaymentTitle)
            .WithOne(title => title.CardBilling)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}