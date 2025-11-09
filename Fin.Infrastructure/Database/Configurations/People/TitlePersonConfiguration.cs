using Fin.Domain.People.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.People;

public class TitlePersonConfiguration: IEntityTypeConfiguration<TitlePerson>
{
    public void Configure(EntityTypeBuilder<TitlePerson> builder)
    {
        builder.HasKey(x => new { x.PersonId, x.TitleId });
        
        builder.Property(x => x.Percentage)
            .HasPrecision(5, 2)
            .IsRequired();
    }
}