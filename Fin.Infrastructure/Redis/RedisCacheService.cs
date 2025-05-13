using System.Text.Json;
using Fin.Infrastructure.AutoServices.Interfaces;
using Microsoft.Extensions.Caching.Distributed;
namespace Fin.Infrastructure.Redis;

public interface IRedisCacheService
{
    public Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null);
    public Task<T?> GetAsync<T>(string key);
    public Task RemoveAsync(string key);
}

public class RedisCacheService: IRedisCacheService, IAutoSingleton
{
    private readonly IDistributedCache _cache;

    public RedisCacheService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null)
    {
        var json = JsonSerializer.Serialize(value);
        options ??= new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
        };

        await _cache.SetStringAsync(key, json, options);
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var json = await _cache.GetStringAsync(key);
        if (string.IsNullOrEmpty(json)) return default;

        return JsonSerializer.Deserialize<T>(json);
    }
    
    public async Task RemoveAsync(string key)
    {
        await _cache.RemoveAsync(key);
    }
}