using RabbitMQ.Client;

namespace Comments.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public const string Exchange = "comments.events";
    public const string DeadLetterExchange = "comments.events.dlx";
    public const string DeadLetterQueue = "comments.dead-letter";

    public static Task DeclareExchangeAsync(IChannel channel, CancellationToken ct) =>
        channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: ct);

    public static async Task DeclareQueueAsync(
        IChannel channel, string queue, string routingKey, CancellationToken ct)
    {
        await DeclareExchangeAsync(channel, ct);

        // «мёртвые письма»: сообщения, которые не удалось обработать, не пропадают, а попадают сюда
        await channel.ExchangeDeclareAsync(DeadLetterExchange, ExchangeType.Fanout, durable: true, cancellationToken: ct);
        await channel.QueueDeclareAsync(DeadLetterQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await channel.QueueBindAsync(DeadLetterQueue, DeadLetterExchange, "", cancellationToken: ct);

        var arguments = new Dictionary<string, object?> { ["x-dead-letter-exchange"] = DeadLetterExchange };
        await channel.QueueDeclareAsync(queue, durable: true, exclusive: false, autoDelete: false,
            arguments: arguments, cancellationToken: ct);
        await channel.QueueBindAsync(queue, Exchange, routingKey, cancellationToken: ct);
    }
    /// <summary>
    /// Временная очередь экземпляра: имя даёт брокер, очередь эксклюзивная и удаляется
    /// при отключении. Так каждый экземпляр API получает свою копию сообщения.
    /// </summary>
    public static async Task<string> DeclareTemporaryQueueAsync(
        IChannel channel, string routingKey, CancellationToken ct)
    {
        await DeclareExchangeAsync(channel, ct);

        var queue = await channel.QueueDeclareAsync("", durable: false, exclusive: true, autoDelete: true,
            cancellationToken: ct);
        await channel.QueueBindAsync(queue.QueueName, Exchange, routingKey, cancellationToken: ct);

        return queue.QueueName;
    }
}