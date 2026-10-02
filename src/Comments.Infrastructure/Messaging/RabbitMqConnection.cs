using RabbitMQ.Client;

namespace Comments.Infrastructure.Messaging;

public sealed record RabbitMqSettings(string Uri);

public sealed class RabbitMqConnection(RabbitMqSettings settings) : IAsyncDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private IConnection? _connection;

    public async Task<IConnection> GetAsync(CancellationToken ct)
    {
        if (_connection is not null)
            return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            // после создания восстановлением обрывов занимается сама библиотека
            return _connection ??= await new ConnectionFactory
            {
                Uri = new Uri(settings.Uri),
                ClientProvidedName = "comments-api",
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            }.CreateConnectionAsync(ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
            await _connection.CloseAsync();
    }
}