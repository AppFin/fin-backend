using System.Security.Cryptography;
using Fin.Infrastructure.AmbientDatas;
using Fin.Infrastructure.Redis;
using Microsoft.Extensions.Caching.Distributed;

namespace Fin.Infrastructure.Notifications.Hubs;

public interface IWebSocketTokenService
{
    Task<string> GenerateConnectionTokenAsync(IAmbientData ambientData);
    Task<WebSocketConnectionToken?> ValidateAndConsumeTokenAsync(string token);
}

public class WebSocketTokenService(IRedisCacheService cache): IWebSocketTokenService
{
    private const int TOKEN_EXPIRATION_MINUTES = 5;
    private const string CACHE_PREFIX = "ws_token:";

    public async Task<string> GenerateConnectionTokenAsync(IAmbientData ambientData)
    {
        if (!ambientData.IsLogged) throw new Exception("Not logged to generate a token.");

        var tokenBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(tokenBytes);
        }

        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var connectionToken = new WebSocketConnectionToken
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(TOKEN_EXPIRATION_MINUTES),
            IsUsed = false,
            UserId = ambientData.UserId.GetValueOrDefault(),
            TenantId = ambientData.TenantId.GetValueOrDefault(),
            DisplayName = ambientData.DisplayName,
            IsAdmin = ambientData.IsAdmin
        };

        var cacheKey = $"{CACHE_PREFIX}{token}";

        await cache.SetAsync(
            cacheKey,
            connectionToken,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(TOKEN_EXPIRATION_MINUTES)
            }
        );

        return token;
    }

    public async Task<WebSocketConnectionToken?> ValidateAndConsumeTokenAsync(string token)
    {
        if (string.IsNullOrEmpty(token)) return null;


        var cacheKey = $"{CACHE_PREFIX}{token}";
        var connectionToken = await cache.GetAsync<WebSocketConnectionToken>(cacheKey);

        if (connectionToken == null || connectionToken.IsUsed || connectionToken.ExpiresAt < DateTime.UtcNow)
        {
            await cache.RemoveAsync(cacheKey);
            return null;
        }

        return connectionToken;
    }
}