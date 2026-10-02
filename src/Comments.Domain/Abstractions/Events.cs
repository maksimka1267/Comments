namespace Comments.Domain.Abstractions;

public interface IEventHandler<in TEvent>
{
    Task HandleAsync(TEvent @event, CancellationToken ct);
}

public interface IEventDispatcher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct);
}