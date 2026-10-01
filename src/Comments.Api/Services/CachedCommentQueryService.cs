using Comments.Api.Contracts;
using Comments.Domain.Abstractions;

namespace Comments.Api.Services;

public sealed class CachedCommentQueryService(CommentQueryService inner, ICacheService cache)
    : ICommentQueryService
{
    // TTL страхует от устаревших данных, если Redis недоступен в момент инвалидации
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(60);

    public async Task<PagedResult<CommentDto>> GetTopLevelAsync(GetCommentsQuery query, CancellationToken ct)
    {
        var version = await cache.GetVersionAsync(CacheScopes.CommentList, ct);
        var key = $"comments:list:v{version}:{query.SortBy}:{query.SortDir}:{query.Page}";

        var cached = await cache.GetAsync<PagedResult<CommentDto>>(key, ct);
        if (cached is not null)
            return cached;

        var result = await inner.GetTopLevelAsync(query, ct);
        await cache.SetAsync(key, result, Ttl, ct);
        return result;
    }

    public Task<CommentDto?> GetByIdAsync(Guid id, CancellationToken ct) =>
        inner.GetByIdAsync(id, ct);
}