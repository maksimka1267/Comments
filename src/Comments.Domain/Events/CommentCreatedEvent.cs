namespace Comments.Domain.Events;

public sealed record CommentCreatedEvent(
    Guid CommentId,
    Guid? ParentId,
    string UserName,
    string Email,
    string Text,
    DateTime CreatedAt);