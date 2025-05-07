namespace Fin.Infrastructure.Authentications.Consts;

public static class AuthenticationConsts
{
    public const string EncryptKeyConfigKey = "ApiSettings:Authentication:Encrypt:Key";
    public const string EncryptIvConfigKey = "ApiSettings:Authentication:Encrypt:Iv";
    
    public const string TokenExpireInMinutesConfigKey = "ApiSettings:Authentication:Jwt:ExpireMinute";
    public const string TokenJwtKeyConfigKey = "ApiSettings:Authentication:Jwt:Key";
    public const string TokenJwtIssuerConfigKey = "ApiSettings:Authentication:Jwt:Issuer";
    public const string TokenJwtAudienceConfigKey = "ApiSettings:Authentication:Jwt:Audience";
}