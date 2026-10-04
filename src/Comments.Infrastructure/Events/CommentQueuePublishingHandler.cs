using Comments.Domain.Abstractions;
using Comments.Domain.Events;

namespace Comments.Infrastructure.Events;

public sealed class CommentQueuePublishingHandler(IMessagePublisher publisher)
    : IEventHandler<CommentCreatedEvent>
{
    public const string RoutingKey = "comment.created";

    public Task HandleAsync(CommentCreatedEvent @event, CancellationToken ct) =>
        publisher.PublishAsync(RoutingKey, @event, ct);
}