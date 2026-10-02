using System.Text.Json;

using Comments.Api.Hubs;
using Comments.Domain.Abstractions;
using Comments.Domain.Events;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Comments.Tests;

public class SignalRRealtimeNotifierTests
{
    [Fact]
    public async Task Broadcasts_only_ids_to_connected_clients()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSignalR();
        builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();

        await using var app = builder.Build();
        app.MapHub<CommentsHub>(CommentsHub.Route);
        await app.StartAsync();

        var server = app.GetTestServer();
        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.BaseAddress, CommentsHub.Route), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => server.CreateHandler();
            })
            .Build();

        var received = new TaskCompletionSource<JsonElement>();
        connection.On<JsonElement>("CommentCreated", payload => received.TrySetResult(payload));
        await connection.StartAsync();

        var commentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var notifier = app.Services.GetRequiredService<IRealtimeNotifier>();
        await notifier.NotifyCommentCreatedAsync(new CommentCreatedEvent(
            commentId, parentId, "user", "secret@example.com", "text", DateTime.UtcNow));

        var result = await received.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.Equal(commentId, result.GetProperty("commentId").GetGuid());
        Assert.Equal(parentId, result.GetProperty("parentId").GetGuid());
        Assert.False(result.TryGetProperty("email", out _));
        Assert.False(result.TryGetProperty("text", out _));
    }
}