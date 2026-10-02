namespace Comments.Api.Contracts;

/// <summary>Что видит браузер: только идентификаторы, без e-mail и текста.</summary>
public sealed record CommentCreatedNotification(Guid CommentId, Guid? ParentId);