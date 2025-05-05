using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Fin.Domain.Global;
using Fin.Domain.Tenants.Entities;
using Fin.Domain.Users.Dtos;
using Fin.Domain.Users.Entities;
using Fin.Infrastructure.Authentications.Dtos;
using Fin.Infrastructure.Authentications.Enums;
using Fin.Infrastructure.AutoServices.Interfaces;
using Fin.Infrastructure.Database.IRepositories;
using Fin.Infrastructure.DateTimes;
using Fin.Infrastructure.Redis;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Fin.Infrastructure.Authentications;

public interface IAuthenticationService
{
    public Task<LoginOutput> Login(LoginInput input);
    public Task<LoginWithGoogleOutput> LoginOrSingInWithGoogle(string userName, string email, string googleId);
    public Task<LoginOutput> RefreshToken(string refreshToken);
    public Task Logout(string token);
}

public class AuthenticationService: IAuthenticationService, IAutoTransient
{
    private readonly IRepository<UserCredential> _credentialRepository;
    private readonly IRepository<User> _userRepository;
    private readonly IRepository<Tenant> _tenantRepository;
    private readonly IRedisCacheService _cache;
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;
    
    private readonly CryptoHelper _cryptoHelper;

    public AuthenticationService(
        IRepository<UserCredential> credentialRepository,
        IConfiguration configuration,
        IRedisCacheService cache,
        IRepository<User> userRepository,
        IDateTimeProvider dateTimeProvider,
        IRepository<Tenant> tenantRepository)
    {
        _credentialRepository = credentialRepository;
        _configuration = configuration;
        _cache = cache;
        _userRepository = userRepository;
        _dateTimeProvider = dateTimeProvider;
        _tenantRepository = tenantRepository;

        var encryptKey = configuration.GetSection("ApiSettings:Authentication:Encrypt:Key").Value ?? "";
        var encryptIv = configuration.GetSection("ApiSettings:Authentication:Encrypt:Iv").Value ?? "";

        _cryptoHelper = new CryptoHelper(encryptKey, encryptIv);
    }

    public async Task<LoginOutput> Login(LoginInput input)
    {
        var encryptedEmail = _cryptoHelper.Encrypt(input.Email);
        var encryptedPassword = _cryptoHelper.Encrypt(input.Password);
        
        var credential = await _credentialRepository.Query()
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

        return await GenerateTokenAsync(credential.User);
    }

    public async Task<LoginWithGoogleOutput> LoginOrSingInWithGoogle(string userName, string email, string googleId)
    {
        var encryptedEmail = _cryptoHelper.Encrypt(email);
        
        var credential = _credentialRepository.Query()
            .Include(c => c.User)
            .ThenInclude(c => c.Tenants)
            .FirstOrDefault(c => c.EncryptedEmail == encryptedEmail);
        
        User user;
        var mustToCreateUser = false;
        if (credential != null)
        {
            user = credential.User;
            if (!user.IsActivity)
                return new LoginWithGoogleOutput { ErrorCode = LoginErrorCode.InactivatedUser };

            if (!credential.HasGoogle)
            {
                credential.GoogleId = googleId;
                await _credentialRepository.UpdateAsync(credential, true);
            }
            else if (credential.GoogleId != googleId)
                return new LoginWithGoogleOutput { ErrorCode = LoginErrorCode.DifferentGoogleAccountLinked };
        }
        else
        {
            mustToCreateUser = true;
            var now = _dateTimeProvider.UtcNow();
            
            var names = userName.Split(" ");
            var firstName = names.FirstOrDefault();
            var lastName = names.Length <= 1 ? "" : names.LastOrDefault();
        
            user = new User(new UserUpdateOrCreateDto
            {
                DisplayName = userName,
                FirstName = firstName,
                LastName = lastName
            }, now);
            credential = new UserCredential(user.Id, encryptedEmail);

            var tenant = new Tenant(now);
            user.Tenants.Add(tenant);
        
            await _tenantRepository.AddAsync(tenant);
            await _userRepository.AddAsync(user);
            await _credentialRepository.AddAsync(credential);
        
        }

        var result = await GenerateTokenAsync(user);
        if (result.Success && mustToCreateUser)
            await _credentialRepository.SaveChangesAsync();
            
        
        return new LoginWithGoogleOutput
        {
            Success = result.Success,
            Token = result.Token,
            RefreshToken = result.RefreshToken,
            ErrorCode = result.ErrorCode,
            MustToCreateUser = result.Success && mustToCreateUser,
        };
    }

    public async Task<LoginOutput> RefreshToken(string refreshToken)
    {
        var userId = await _cache.GetAsync<Guid>(GetRefreshTokenCacheKey(refreshToken));
        if (userId == Guid.Empty)
            return new LoginOutput { ErrorCode = LoginErrorCode.InvalidRefreshToken };
        
        var user = await _userRepository.Query(false)
            .Include(c => c.Tenants)
            .FirstOrDefaultAsync(c => c.Id == userId);
        if (user == null)
            return new LoginOutput { ErrorCode = LoginErrorCode.InvalidRefreshToken };

        await _cache.RemoveAsync(GetRefreshTokenCacheKey(refreshToken));
        return await GenerateTokenAsync(user);
    }

    public async Task Logout(string token)
    {
        var refreshToken  = await _cache.GetAsync<string>(GetTokenRefreshCacheKey(token));
        await _cache.RemoveAsync(GetRefreshTokenCacheKey(refreshToken));
        await _cache.RemoveAsync(GetTokenRefreshCacheKey(token));

        var now = _dateTimeProvider.UtcNow();

        var expiration = GetExpirationFromToken(token) ??
                         now.AddMinutes(double.Parse(_configuration.GetSection("ApiSettings:Authentication:Jwt:ExpireMinutes").Value ?? "120"));

        await _cache.SetAsync(GetLogoutTokenCacheKey(token), true, new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiration
        });
    }

    public DateTime? GetExpirationFromToken(string token)
    {
        var handler = new JwtSecurityTokenHandler();

        if (!handler.CanReadToken(token))
            return null;

        var jwtToken = handler.ReadJwtToken(token);

        return jwtToken.ValidTo; 
    }
    
    private async Task<LoginOutput> GenerateTokenAsync(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration.GetSection("ApiSettings:Authentication:Jwt:Key").Value ?? "");
        
        var minutesToExpire = double.Parse(_configuration.GetSection("ApiSettings:Authentication:Jwt:ExpireMinutes").Value ?? "120");
        var expiration = _dateTimeProvider.UtcNow().AddMinutes(minutesToExpire);

        var credent = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature);

        var issuer = _configuration.GetSection("ApiSettings:Authentication:Jwt:Issuer").Value ?? "";
        var audience = _configuration.GetSection("ApiSettings:Authentication:Jwt:Audience").Value ?? "";

        var subject = new List<Claim>();
        subject.Add(new Claim(ClaimTypes.Name, user.DisplayName));
        subject.Add(new Claim("userId", user.Id.ToString()));
        subject.Add(new Claim(ClaimTypes.Role, user.IsAdmin ? "Admin" : "User"));
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