using Comments.Domain.Abstractions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Comments.Infrastructure.Events;

public sealed class InProcessEventDispatcher(
    IServiceProvider services,
    ILogger<InProcessEventDispatcher> logger) : IEventDispatcher
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
    {
        // обработчики вызываются в порядке регистрации
        foreach (var handler in services.GetServices<IEventHandler<TEvent>>())
        {
            try
            {
                await handler.HandleAsync(@event, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Event handler {Handler} failed for {Event}",
                    handler.GetType().Name, typeof(TEvent).Name);
            }
        }
    }
}