using Fin.Domain.CreditCharges.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.CreditCharges;

public class CreditChargeConfiguration: IEntityTypeConfiguration<CreditCharge>
{
    public void Configure(EntityTypeBuilder<CreditCharge> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(100).IsRequired();
        
        builder
            .Property(charge => charge.Value)
            .HasColumnType("numeric(19,4)")
            .HasPrecision(19, 4);
        builder
            .HasOne(charge => charge.CreditCard)
            .WithMany(card => card.CreditCharges)
            .HasForeignKey(charge => charge.CreditCardId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}