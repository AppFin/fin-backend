using Fin.Domain.People.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Fin.Infrastructure.Database.Configurations.People;

public class PeopleConfiguration: IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => new {x.Name, x.TenantId}).IsUnique();
        
        builder
            .HasMany(x => x.Titles)
            .WithMany(x => x.People)
            .UsingEntity<TitlePerson>(
                l => l
                    .HasOne(ttc => ttc.Title)
                    .WithMany(title => title.TitlePeople)
                    .HasForeignKey(e => e.TitleId) 
                    .OnDelete(DeleteBehavior.Cascade),
                r => r
                    .HasOne(ttc => ttc.Person)
                    .WithMany(category => category.TitlePeople)
                    .HasForeignKey(e => e.PersonId)
                    .OnDelete(DeleteBehavior.Cascade)
            );
    }
}