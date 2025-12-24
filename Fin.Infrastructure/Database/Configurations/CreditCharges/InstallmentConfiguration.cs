using Fin.Domain.CreditCharges.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.CreditCharges;

public class InstallmentConfiguration: IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.HasKey(x => x.Id);
        
        builder
            .Property(installment => installment.Value)
            .HasColumnType("numeric(19,4)")
            .HasPrecision(19, 4);
        
        builder
            .HasOne(installment => installment.CreditCharge)
            .WithMany(charge => charge.Installments)
            .HasForeignKey(installment => installment.CreditChargeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne(installment => installment.CardBilling)
            .WithMany(installment => installment.Installments)
            .HasForeignKey(installment => installment.CardBillingId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}