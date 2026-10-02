using Comments.Domain.Abstractions;
using Comments.Domain.Events;
using Comments.Infrastructure.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;

namespace Comments.Infrastructure.Messaging;

/// <summary>
/// Слушает событие создания комментария и рассылает его браузерам. Очередь у каждого
/// экземпляра API своя (временная), поэтому уведомление получают клиенты всех экземпляров
/// и отдельный backplane для SignalR не нужен.
/// </summary>
public sealed class CommentRealtimeConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory scopes,
    ILogger<CommentRealtimeConsumer> logger)
    : RabbitMqConsumer<CommentCreatedEvent>(connection, scopes, logger)
{
    protected override string QueueName => "comments.realtime (temporary, per instance)";
    protected override string RoutingKey => CommentQueuePublishingHandler.RoutingKey;

    protected override Task<string> DeclareQueueAsync(IChannel channel, CancellationToken ct) =>
        RabbitMqTopology.DeclareTemporaryQueueAsync(channel, RoutingKey, ct);

    protected override Task HandleAsync(IServiceProvider services, CommentCreatedEvent message, CancellationToken ct)
    {
        Logger.LogInformation("Realtime: broadcasting comment {CommentId}", message.CommentId);
        return services.GetRequiredService<IRealtimeNotifier>().NotifyCommentCreatedAsync(message, ct);
    }
}