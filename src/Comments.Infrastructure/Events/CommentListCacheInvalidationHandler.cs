using Comments.Domain.Abstractions;
using Comments.Domain.Events;

namespace Comments.Infrastructure.Events;

public sealed class CommentListCacheInvalidationHandler(ICacheService cache)
    : IEventHandler<CommentCreatedEvent>
{
    public Task HandleAsync(CommentCreatedEvent @event, CancellationToken ct) =>
        cache.BumpVersionAsync(CacheScopes.CommentList, ct);
}