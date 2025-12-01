using Fin.Infrastructure.EmailSenders.MailSender;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.EmailSenders.MailKit;

public static class MailKitClientExtension
{
    public static IServiceCollection AddMailKitClient(this IServiceCollection services)
    {
        services.AddHttpClient<IMailKitClient, MailKitClient>();
        
        return services;
    }
}