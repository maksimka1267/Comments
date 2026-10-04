namespace Comments.Api.Contracts;

public sealed record SearchHitDto(
    Guid Id,
    Guid? ParentId,
    string UserName,
    string Snippet,
    DateTime CreatedAt);