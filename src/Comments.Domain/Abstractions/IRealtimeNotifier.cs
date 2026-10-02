using Comments.Domain.Events;

namespace Comments.Domain.Abstractions;

/// <summary>Отправляет уведомления подключённым браузерам в реальном времени.</summary>
public interface IRealtimeNotifier
{
    Task NotifyCommentCreatedAsync(CommentCreatedEvent comment, CancellationToken ct = default);
}