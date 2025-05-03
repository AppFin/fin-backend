using Fin.Application.AutoServices.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using IAutoScoped = Fin.Application.AutoServices.Interfaces.IAutoScoped;

namespace Fin.Application.AutoServices.Extensions;

public static class AddAutoServicesExtension
{
    public static IServiceCollection AddAutoTransientServices(this IServiceCollection services)
    {
        RegisterDependencyByType(services, typeof(IAutoTransient), ServiceLifetime.Transient);
        return services;
    }
    
    public static IServiceCollection AddAutoScopedServices(this IServiceCollection services)
    {
        RegisterDependencyByType(services, typeof(IAutoScoped), ServiceLifetime.Scoped);
        return services;
    }
    
    public static IServiceCollection AddAutoSingletonServices(this IServiceCollection services)
    {
        RegisterDependencyByType(services, typeof(IAutoSingleton), ServiceLifetime.Singleton);
        return services;
    }
    
    private static void RegisterDependencyByType(IServiceCollection serviceCollection, Type dependencyType, ServiceLifetime lifeStyle)
    {
        var dependencies =
            from dependency in AppDomain.CurrentDomain.GetAssemblies().SelectMany(assembly => assembly.GetTypes())
            where dependency.GetInterfaces().Contains(dependencyType)
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
                var serviceDescriptor = new ServiceDescriptor(dependency.ServiceType, implementationType, lifeStyle);
                serviceCollection.Add(serviceDescriptor);
            }
        }
    }
    
    private static Type GetDirectImplementedInterfaceFromType(this Type type)
    {
        return type.BaseType != null
            ? type.GetInterfaces().Except(type.BaseType.GetInterfaces()).First()
            : type.GetInterfaces().First();
    }
}