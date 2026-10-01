using System.Text.Json;

using Comments.Domain.Abstractions;

using Microsoft.Extensions.Logging;

using StackExchange.Redis;

namespace Comments.Infrastructure.Caching;

public sealed class RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    : ICacheService
{
    private static string VersionKey(string scope) => $"version:{scope}";

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct)
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(key);
            return value.HasValue ? JsonSerializer.Deserialize<T>((string)value!) : default;
        }
        catch (Exception ex) when (ex is RedisException or JsonException)
        {
            logger.LogWarning(ex, "Cache read failed for {Key}", key);
            return default;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        try
        {
            await redis.GetDatabase().StringSetAsync(key, JsonSerializer.Serialize(value), ttl);
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Cache write failed for {Key}", key);
        }
    }

    public async Task<long> GetVersionAsync(string scope, CancellationToken ct)
    {
        try
        {
            var value = await redis.GetDatabase().StringGetAsync(VersionKey(scope));
            return value.HasValue ? (long)value : 0;
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Cache version read failed for {Scope}", scope);
            return 0;
        }
    }

    public async Task BumpVersionAsync(string scope, CancellationToken ct)
    {
        try
        {
            await redis.GetDatabase().StringIncrementAsync(VersionKey(scope));
        }
        catch (RedisException ex)
        {
            logger.LogWarning(ex, "Cache invalidation failed for {Scope}", scope);
        }
    }
}