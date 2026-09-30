namespace Comments.Api.Contracts;

public sealed record CreateCommentRequest(
    string UserName,
    string Email,
    string? HomePage,
    string Text,
    Guid? ParentId);