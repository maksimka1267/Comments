using Comments.Domain.Abstractions;

using Microsoft.Extensions.Caching.Memory;

namespace Comments.Infrastructure.Captcha;

public sealed class MemoryCaptchaStore(IMemoryCache cache) : ICaptchaStore
{
    private static string Key(Guid id) => $"captcha:{id}";

    public Task SetAsync(Guid id, string answer, TimeSpan ttl, CancellationToken ct)
    {
        cache.Set(Key(id), answer, ttl);
        return Task.CompletedTask;
    }

    public Task<string?> TakeAsync(Guid id, CancellationToken ct)
    {
        if (cache.TryGetValue(Key(id), out string? answer))
        {
            cache.Remove(Key(id));
            return Task.FromResult(answer);
        }

        return Task.FromResult<string?>(null);
    }
}