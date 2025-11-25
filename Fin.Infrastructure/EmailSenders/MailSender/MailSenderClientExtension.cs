using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.EmailSenders.MailSender;

public static class MailSenderClientExtension
{
    public static IServiceCollection AddMailSenderClient(this IServiceCollection services)
    {
        services.AddHttpClient<IMailSenderClient, MailSenderClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.mailersend.com/v1/email");
        });
        
        return services;
    }
}