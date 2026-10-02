using Comments.Api.Contracts;
using Comments.Domain.Abstractions;
using Comments.Domain.Events;

using Microsoft.AspNetCore.SignalR;

namespace Comments.Api.Hubs;

public sealed class SignalRRealtimeNotifier(IHubContext<CommentsHub, ICommentsClient> hub)
    : IRealtimeNotifier
{
    public Task NotifyCommentCreatedAsync(CommentCreatedEvent comment, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return hub.Clients.All.CommentCreated(
            new CommentCreatedNotification(comment.CommentId, comment.ParentId));
    }
}