using Comments.Domain.Events;
using Comments.Infrastructure.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Messaging;

public sealed class CommentAuditConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopes,
    ILogger<CommentAuditConsumer> logger)
    : RabbitMqConsumer<CommentCreatedEvent>(connection, scopes, logger)
{
    protected override string QueueName => "comments.audit";
    protected override string RoutingKey => CommentQueuePublishingHandler.RoutingKey;

    protected override Task HandleAsync(IServiceProvider services, CommentCreatedEvent message, CancellationToken ct)
    {
        Logger.LogInformation(
            "Audit: comment {CommentId} by {UserName} created at {CreatedAt} (reply to {ParentId})",
            message.CommentId, message.UserName, message.CreatedAt, message.ParentId);

        return Task.CompletedTask;
    }
}