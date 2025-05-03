using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Database.Configurations;

public static class TenantEntityConfiguration
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(u =>
        {
            u.HasKey(x => x.Id);
            
            
            u
                .HasMany(e => e.Users)
                .WithMany(e => e.Tenants)
                .UsingEntity<TenantUser>(
                    l => l.HasOne<User>().WithMany().HasForeignKey(e => e.UserId),
                    r => r.HasOne<Tenant>().WithMany().HasForeignKey(e => e.TenantId));
        });

        modelBuilder.Entity<TenantUser>(u =>
        {
            u.HasKey(x => new { x.TenantId, x.UserId });
        });
    }
}