using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Fin.Infrastructure.Firebases;

public static class AddFirebaseExtension
{
    /// Push notifications are optional infra: skip silently instead of crashing
    /// the whole app when no real Firebase service account is configured
    /// (e.g. the portfolio demo, which ships only placeholder credentials).
    public static IServiceCollection AddFirebase(this IServiceCollection services, IConfiguration configuration)
    {
        var serverKey = configuration.GetSection(FirebaseConsts.FirebaseServerKeyKey).Value;

        try
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(serverKey)
            });
        }
        catch (Exception ex)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            loggerFactory.CreateLogger(nameof(AddFirebaseExtension))
                .LogWarning(ex, "Firebase was not initialized: no valid service account configured. Push notifications will be disabled.");
        }

        return services;
    }
}