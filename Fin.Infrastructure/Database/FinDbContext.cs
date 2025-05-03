using System.Linq.Expressions;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Database.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Fin.Infrastructure.Database;

public class FinDbContext: DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserCredential> Credentials { get; set; }
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantUser> TenantUsers { get; set; }
    
    public FinDbContext()
    {
    }
    
    public FinDbContext(DbContextOptions<FinDbContext> options, bool migrate = true) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("public");
        
        UserEntityConfiguration.Configure(modelBuilder);
        TenantEntityConfiguration.Configure(modelBuilder);
     
        // Configurar quando tiver o AmbientData
        // ConfigTenantFilter(modelBuilder);
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseNpgsql();
        }
    }

    private void ConfigTenantFilter(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                modelBuilder.Entity(entityType.ClrType)
                    .HasQueryFilter(BuildTenantFilter(entityType.ClrType));
            }
        }
    }
    
    private static LambdaExpression BuildTenantFilter(Type entityType)
    {
        var parameter = Expression.Parameter(entityType, "e");
        var property = Expression.Property(parameter, nameof(ITenantEntity.TenantId));
        var comparison = Expression.Equal(property, Expression.Constant(Guid.NewGuid()));
        return Expression.Lambda(comparison, parameter);
    }
}