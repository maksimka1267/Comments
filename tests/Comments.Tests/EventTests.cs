using Comments.Domain.Abstractions;
using Comments.Domain.Events;
using Comments.Infrastructure.Events;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Comments.Tests;

public class EventTests
{
    private static readonly CommentCreatedEvent Sample =
        new(Guid.NewGuid(), null, "Anna", "anna@example.com", "text", DateTime.UtcNow);

    private sealed class RecordingHandler : IEventHandler<CommentCreatedEvent>
    {
        public int Calls;

        public Task HandleAsync(CommentCreatedEvent @event, CancellationToken ct)
        {
            Calls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FailingHandler : IEventHandler<CommentCreatedEvent>
    {
        public Task HandleAsync(CommentCreatedEvent @event, CancellationToken ct) =>
            throw new InvalidOperationException("boom");
    }

    private static IEventDispatcher CreateDispatcher(params IEventHandler<CommentCreatedEvent>[] handlers)
    {
        var services = new ServiceCollection();
        foreach (var handler in handlers)
            services.AddSingleton(handler);

        var provider = services.BuildServiceProvider();
        return new InProcessEventDispatcher(provider, NullLogger<InProcessEventDispatcher>.Instance);
    }

    [Fact]
    public async Task All_handlers_receive_the_event()
    {
        var first = new RecordingHandler();
        var second = new RecordingHandler();

        await CreateDispatcher(first, second).PublishAsync(Sample, default);

        Assert.Equal(1, first.Calls);
        Assert.Equal(1, second.Calls);
    }

    [Fact]
    public async Task Failing_handler_does_not_stop_the_others()
    {
        var after = new RecordingHandler();

        await CreateDispatcher(new FailingHandler(), after).PublishAsync(Sample, default);

        Assert.Equal(1, after.Calls);
    }

    [Fact]
    public async Task Cache_handler_bumps_the_list_version()
    {
        var cache = new InMemoryCacheService();
        var handler = new CommentListCacheInvalidationHandler(cache);

        await handler.HandleAsync(Sample, default);

        Assert.Equal(1, await cache.GetVersionAsync(CacheScopes.CommentList, default));
    }
}