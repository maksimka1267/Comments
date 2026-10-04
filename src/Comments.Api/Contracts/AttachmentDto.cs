using Comments.Domain.Entities;

namespace Comments.Api.Contracts;

public sealed record AttachmentDto(
    Guid Id,
    AttachmentKind Kind,
    string FileName,
    string ContentType,
    long SizeBytes);