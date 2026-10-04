using Comments.Domain.Abstractions;

using StackExchange.Redis;

namespace Comments.Infrastructure.Captcha;

public sealed class RedisCaptchaStore(IConnectionMultiplexer redis) : ICaptchaStore
{
    private static string Key(Guid id) => $"captcha:{id:N}";

    public Task SetAsync(Guid id, string answer, TimeSpan ttl, CancellationToken ct) =>
        redis.GetDatabase().StringSetAsync(Key(id), answer, ttl);

    public async Task<string?> TakeAsync(Guid id, CancellationToken ct)
    {
        // GETDEL читает и удаляет ключ одной атомарной командой: капча одноразовая
        // даже если два запроса пришли одновременно
        var value = await redis.GetDatabase().StringGetDeleteAsync(Key(id));
        return value.HasValue ? (string?)value : null;
    }
}