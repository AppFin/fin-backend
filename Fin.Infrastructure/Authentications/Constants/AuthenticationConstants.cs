namespace Fin.Infrastructure.Authentications.Constants;

public static class AuthenticationConstants
{
    public const string EncryptKeyConfigKey = "ApiSettings:Authentication:Encrypt:Key";
    public const string EncryptIvConfigKey = "ApiSettings:Authentication:Encrypt:Iv";
    
    public const string TokenExpireInMinutesConfigKey = "ApiSettings:Authentication:Jwt:ExpireMinutes";
    public const string TokenJwtKeyConfigKey = "ApiSettings:Authentication:Jwt:Key";
    public const string TokenJwtIssuerConfigKey = "ApiSettings:Authentication:Jwt:Issuer";
    public const string TokenJwtAudienceConfigKey = "ApiSettings:Authentication:Jwt:Audience";
    
    public const string GoogleClientIdConfigKey = "ApiSettings:Authentication:Google:ClientId";
    public const string GoogleClientSecretConfigKey = "ApiSettings:Authentication:Google:ClientSecret";
}