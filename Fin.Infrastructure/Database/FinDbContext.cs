using System.Reflection;
using Fin.Domain.Global.Interfaces;
using Fin.Domain.Notifications;
using Fin.Domain.Notifications.Entities;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Database.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Fin.Infrastructure.Database;

public class FinDbContext : DbContext
{
    public DbSet<User> Users { get; set; }
    public DbSet<UserCredential> Credentials { get; set; }
    
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantUser> TenantUsers { get; set; }
    
    public DbSet<UserNotificationSettings> UserNotificationSettings { get; set; }
    public DbSet<UserRememberUseSetting> UserRememberUseSettings { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<NotificationUserDelivery> NotificationUserDeliveries { get; set; }

    private readonly IAmbientData _ambientData;

    public FinDbContext()
    {
    }

    public FinDbContext(DbContextOptions<FinDbContext> options, IAmbientData ambientData) :
        base(options)
    {
        _ambientData = ambientData;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("public");

        UserEntityConfiguration.Configure(modelBuilder);
        TenantEntityConfiguration.Configure(modelBuilder);
        NotificationEntityConfiguration.Configure(modelBuilder);

        ApplyTenantFilter(modelBuilder);
        ApplyUtcConverterToDateTime(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseNpgsql();
        }
    }

    private void ApplyTenantFilter(ModelBuilder modelBuilder)
    {
        if (!(_ambientData?.IsLogged ?? false)) return;
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(FinDbContext)
                    .GetMethod(nameof(SetTenantFilter), BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.MakeGenericMethod(entityType.ClrType);

                method?.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void SetTenantFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : class, ITenantEntity
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.TenantId == _ambientData.TenantId);
    }

    private void ApplyUtcConverterToDateTime(ModelBuilder modelBuilder)
    {
        if (Database.ProviderName != "Microsoft.EntityFrameworkCore.Sqlite") return;

        var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTime))
                {
                    property.SetValueConverter(dateTimeConverter);
                }
            }
        }
    }
}