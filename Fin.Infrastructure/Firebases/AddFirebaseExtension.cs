using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Fin.Infrastructure.Firebases;

public static class AddFirebaseExtension
{
    public static IServiceCollection AddFirebase(this IServiceCollection services, IConfiguration configuration)
    {
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromJson(configuration.GetSection(FirebaseConsts.FirebaseServerKeyKey).Value)
        });
        return services;
    }
}