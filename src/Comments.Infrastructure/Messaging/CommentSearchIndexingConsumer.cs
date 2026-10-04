using Comments.Domain.Abstractions;
using Comments.Domain.Events;
using Comments.Infrastructure.Events;
using Comments.Infrastructure.Search;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Messaging;

/// <summary>Кладёт новые комментарии в поисковый индекс Elasticsearch.</summary>
public sealed class CommentSearchIndexingConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopes,
    ILogger<CommentSearchIndexingConsumer> logger)
    : RabbitMqConsumer<CommentCreatedEvent>(connection, scopes, logger)
{
    protected override string QueueName => "comments.search";
    protected override string RoutingKey => CommentQueuePublishingHandler.RoutingKey;

    protected override async Task HandleAsync(
        IServiceProvider services, CommentCreatedEvent message, CancellationToken ct)
    {
        var index = services.GetRequiredService<ICommentSearchIndex>();

        await index.IndexAsync(SearchDocuments.FromEvent(message), ct);

        Logger.LogInformation("Search: comment {CommentId} indexed", message.CommentId);
    }
}