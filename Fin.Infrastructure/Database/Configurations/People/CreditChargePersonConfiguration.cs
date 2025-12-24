using Fin.Domain.People.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.People;

public class CreditChargePersonConfiguration: IEntityTypeConfiguration<CreditChargePerson>
{
    public void Configure(EntityTypeBuilder<CreditChargePerson> builder)
    {
        builder.HasKey(x => new { x.PersonId, x.CreditChargeId });
        
        builder.Property(x => x.Percentage)
            .HasPrecision(5, 2)
            .IsRequired();
    }
}