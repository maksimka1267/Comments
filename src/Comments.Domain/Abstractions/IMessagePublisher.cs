namespace Comments.Domain.Abstractions;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string routingKey, T message, CancellationToken ct);
}