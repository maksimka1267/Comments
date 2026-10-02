using System.Text.Json;

using Comments.Domain.Abstractions;

using RabbitMQ.Client;

namespace Comments.Infrastructure.Messaging;

public sealed class RabbitMqPublisher(RabbitMqConnection connection) : IMessagePublisher, IAsyncDisposable
{
    // канал не потокобезопасен, поэтому публикации идут по очереди
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IChannel? _channel;

    public async Task PublishAsync<T>(string routingKey, T message, CancellationToken ct)
    {
        var body = JsonSerializer.SerializeToUtf8Bytes(message);
        var properties = new BasicProperties { Persistent = true, ContentType = "application/json" };

        await _lock.WaitAsync(ct);
        try
        {
            var channel = await GetChannelAsync(ct);
            await channel.BasicPublishAsync(RabbitMqTopology.Exchange, routingKey, mandatory: false, properties, body, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IChannel> GetChannelAsync(CancellationToken ct)
    {
        if (_channel is { IsOpen: true })
            return _channel;

        var conn = await connection.GetAsync(ct);
        _channel = await conn.CreateChannelAsync(
            new CreateChannelOptions(publisherConfirmationsEnabled: true, publisherConfirmationTrackingEnabled: true),
            ct);
        await RabbitMqTopology.DeclareExchangeAsync(_channel, ct);
        return _channel;
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
            await _channel.CloseAsync();
    }
}