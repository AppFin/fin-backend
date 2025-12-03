using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.EmailSenders.MailKit;

public static class MailKitClientExtension
{
    public static IServiceCollection AddMailKitClient(this IServiceCollection services)
    {
        services.AddSingleton<IMailKitClient, MailKitClient>();
        
        return services;
    }
}