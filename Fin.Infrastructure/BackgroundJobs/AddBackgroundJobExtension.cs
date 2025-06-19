using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace Fin.Infrastructure.BackgroundJobs;

public static class AddBackgroundJobExtension
{
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHangfire(config =>
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(c => c.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection")))
        ).AddHangfireServer()
        .RegisterRecurringBackgroundJobs()
        .AddHostedService<RecurringBackgroundJobHostedService>();

        return services;
    }

    private static IServiceCollection RegisterRecurringBackgroundJobs(this IServiceCollection serviceCollection)
    {
        var dependencies =
            from dependency in AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
            where dependency.GetInterfaces().Contains(typeof(IAsyncRecurringBackgroundJob))
            group dependency by dependency.GetDirectImplementedInterfaceFromType()
            into groupedDependencies
            select new
            {
                HasMoreThanOneClassImplementingService = groupedDependencies.Count() > 1,
                ServiceType = groupedDependencies.Key,
                ImplementationTypes = groupedDependencies
            };

        foreach (var dependency in dependencies)
        {
            foreach (var implementationType in dependency.ImplementationTypes)
            {
                var serviceDescriptorClass = new ServiceDescriptor(implementationType, implementationType, ServiceLifetime.Scoped);
                serviceCollection.Add(serviceDescriptorClass);

                var serviceDescriptorInterface = new ServiceDescriptor(typeof(IAsyncRecurringBackgroundJob), implementationType, ServiceLifetime.Scoped);
                serviceCollection.Add(serviceDescriptorInterface);
            }
        }

        return serviceCollection;
    }

    private static Type GetDirectImplementedInterfaceFromType(this Type type)
    {
        return type.BaseType != null
            ? type.GetInterfaces().Except(type.BaseType.GetInterfaces()).First()
            : type.GetInterfaces().First();
    }
}