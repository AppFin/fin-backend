using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fin.Domain.Global;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Authentications.Constants;
using Fin.Infrastructure.Authentications.Dtos;
using Fin.Infrastructure.Authentications.Enums;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.Repositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Fin.Infrastructure.Authentications;

public interface IAuthenticationTokenService
{
    public Task<LoginOutput> Login(LoginInput input);
    public Task<LoginOutput> GenerateTokenAsync(UserDto user);
    public Task<LoginOutput> RefreshToken(string refreshToken);
    public Task Logout(string token);
}

public class AuthenticationTokenService: IAuthenticationTokenService, IAutoTransient
{
    private readonly IRepository<UserCredential> _credentialRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRedisCacheService _cache;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    
    private readonly CryptoHelper _cryptoHelper;

    public AuthenticationTokenService(
        IRepository<UserCredential> credentialRepository,
        IRepository<User> userRepository,
        IConfiguration configuration,
        IRedisCacheService cache,
        IDateTimeProvider dateTimeProvider)
    {
        _credentialRepository = credentialRepository;
        _configuration = configuration;
        _cache = cache;
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;

        var encryptKey = configuration.GetSection(AuthenticationConstants.EncryptKeyConfigKey).Value ?? "";
        var encryptIv = configuration.GetSection(AuthenticationConstants.EncryptIvConfigKey).Value ?? "";

        _cryptoHelper = new CryptoHelper(encryptKey, encryptIv);
    }

    public async Task<LoginOutput> Login(LoginInput input)
    {
        var encryptedEmail = _cryptoHelper.Encrypt(input.Email);
        var encryptedPassword = _cryptoHelper.Encrypt(input.Password);
        
        var credential = await _credentialRepository
            .Include(c => c.User)
            .ThenInclude(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.EncryptedEmail == encryptedEmail);

        if (credential == null)
            return new LoginOutput { ErrorCode = LoginErrorCode.EmailNotFound };
        if (credential.ExceededAttempts)
            return new LoginOutput { ErrorCode = LoginErrorCode.MaxAttemptsReached };
        if (!credential.User.IsActivity)
            return new LoginOutput { ErrorCode = LoginErrorCode.InactivatedUser };
        if (!credential.HasPassword)
            return new LoginOutput { ErrorCode = LoginErrorCode.DoNotHasPassword };
        
        var validPassword = credential.TestCredentials(encryptedEmail, encryptedPassword); 
        
        await _credentialRepository.UpdateAsync(credential, true);
        if (!validPassword)
            return new LoginOutput { ErrorCode = LoginErrorCode.InvalidPassword };

        return await GenerateTokenAsync(new UserDto(credential.User));
    }
    public async Task<LoginOutput> RefreshToken(string refreshToken)
    {
        var userId = await _cache.GetAsync<Guid>(GetRefreshTokenCacheKey(refreshToken));
        if (userId == Guid.Empty)
            return new LoginOutput { ErrorCode = LoginErrorCode.InvalidRefreshToken };
        
        var user = await _userRepository.AsNoTracking()
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == userId);
        if (user == null)
            return new LoginOutput { ErrorCode = LoginErrorCode.InvalidRefreshToken };
        if (!user.IsActivity)
            return new LoginOutput { ErrorCode = LoginErrorCode.InactivatedUser };

        await _cache.RemoveAsync(GetRefreshTokenCacheKey(refreshToken));
        return await GenerateTokenAsync(new UserDto(user));
    }

    public async Task Logout(string token)
    {
        var refreshToken  = await _cache.GetAsync<string>(GetTokenRefreshCacheKey(token));
        await _cache.RemoveAsync(GetRefreshTokenCacheKey(refreshToken));
        await _cache.RemoveAsync(GetTokenRefreshCacheKey(token));

        var now = _dateTimeProvider.UtcNow();

        var expiration = GetExpirationFromToken(token) ??
                         now.AddMinutes(double.Parse(_configuration.GetSection(AuthenticationConstants.TokenExpireInMinutesConfigKey).Value ?? "120"));

        await _cache.SetAsync(GetLogoutTokenCacheKey(token), true, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiration
        });
    }
    
    public async Task<LoginOutput> GenerateTokenAsync(UserDto user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration.GetSection(AuthenticationConstants.TokenJwtKeyConfigKey).Value ?? "");
        
        var minutesToExpire = double.Parse(_configuration.GetSection(AuthenticationConstants.TokenExpireInMinutesConfigKey).Value ?? "120");
        var expiration = _dateTimeProvider.UtcNow().AddMinutes(minutesToExpire);

        var credent = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

        var issuer = _configuration.GetSection(AuthenticationConstants.TokenJwtIssuerConfigKey).Value ?? "";
        var audience = _configuration.GetSection(AuthenticationConstants.TokenJwtAudienceConfigKey).Value ?? "";

        var subject = new List<Claim>();
        subject.Add(new Claim(ClaimTypes.Name, user.DisplayName));
        subject.Add(new Claim("userId", user.Id.ToString()));
        subject.Add(new Claim(ClaimTypes.Role, user.IsAdmin ? AuthenticationRoles.Admin : AuthenticationRoles.User));
        subject.Add(new Claim("imageUrl", user.ImagePublicUrl ?? ""));
        subject.Add(new Claim("tenantId", user.Tenants.FirstOrDefault()?.Id.ToString() ?? ""));
        
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(subject),
            Expires = expiration,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credent
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(token);

        return new LoginOutput
        {
            Success = true,
            Token = tokenString,
            RefreshToken = await GenerateRefreshTokenAsync(user.Id, tokenString)
        };
    }
    
    private DateTime? GetExpirationFromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(token))
            return null;

        var jwtToken = handler.ReadJwtToken(token);

        return jwtToken.ValidTo; 
    }
    
    private async Task<string> GenerateRefreshTokenAsync(Guid userId, string token)
    {
        var refreshToken = $"{Guid.NewGuid()}-{Guid.NewGuid()}";
        var expiration = TimeSpan.FromDays(7);

        var opetions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration,
        };
        
        await _cache.SetAsync(GetTokenRefreshCacheKey(token), refreshToken, opetions);
        await _cache.SetAsync(GetRefreshTokenCacheKey(refreshToken), userId, opetions);

        return refreshToken;
    }
    
    private static string GetTokenRefreshCacheKey(string token) => $"token-refresh-{token}";
    private static string GetRefreshTokenCacheKey(string refreshToken) => $"refresh-token-{refreshToken}";
    public static string GetLogoutTokenCacheKey(string token) => $"logged-out-{token}";
}