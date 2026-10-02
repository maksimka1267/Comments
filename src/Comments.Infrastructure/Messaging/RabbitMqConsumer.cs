using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Comments.Infrastructure.Messaging;

public abstract class RabbitMqConsumer<TMessage>(
    RabbitMqConnection connection,
    IServiceScopeFactory scopes,
    ILogger logger) : BackgroundService
{
    protected ILogger Logger => logger;

    protected abstract string QueueName { get; }
    protected abstract string RoutingKey { get; }

    protected abstract Task HandleAsync(IServiceProvider services, TMessage message, CancellationToken ct);
    // по умолчанию: именованная устойчивая очередь с dead-letter; потомок может переопределить
    protected virtual async Task<string> DeclareQueueAsync(IChannel channel, CancellationToken ct)
    {
        await RabbitMqTopology.DeclareQueueAsync(channel, QueueName, RoutingKey, ct);
        return QueueName;
    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(stoppingToken);
                return;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // брокер может подниматься дольше API: пробуем снова
                logger.LogWarning(ex, "Consumer of {Queue} could not start, retrying in 5 seconds", QueueName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(CancellationToken ct)
    {
        var conn = await connection.GetAsync(ct);
        await using var channel = await conn.CreateChannelAsync(cancellationToken: ct);

        var queue = await DeclareQueueAsync(channel, ct);
        await channel.BasicQosAsync(0, 10, false, ct); // не больше 10 необработанных сообщений за раз

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += async (_, ea) =>
        {
            try
            {
                var message = JsonSerializer.Deserialize<TMessage>(ea.Body.Span)
                              ?? throw new JsonException("Empty message.");

                using var scope = scopes.CreateScope();
                await HandleAsync(scope.ServiceProvider, message, ct);

                await channel.BasicAckAsync(ea.DeliveryTag, false, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Failed to process a message from {Queue}", QueueName);

                // без повторной постановки: сообщение уйдёт в dead-letter очередь
                await channel.BasicNackAsync(ea.DeliveryTag, false, requeue: false, ct);
            }
        };

        await channel.BasicConsumeAsync(queue, autoAck: false, consumer, ct);

        // держим канал открытым, пока работает приложение
        await Task.Delay(Timeout.Infinite, ct);
    }
}