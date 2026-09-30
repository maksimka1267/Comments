namespace Comments.Api.Contracts;

public sealed record CommentDto(
    Guid Id,
    Guid? ParentId,
    string UserName,
    string Email,
    string? HomePage,
    string Text,
    DateTime CreatedAt,
    AttachmentDto? Attachment)
{
    public List<CommentDto> Replies { get; init; } = [];
}