using Comments.Api.Contracts;

namespace Comments.Api.Services;

public sealed record ClientInfo(string IpAddress, string UserAgent);

public sealed record UploadedFile(string FileName, byte[] Content);

public enum CreateCommentStatus { Created, ParentNotFound, EmptyText, InvalidCaptcha, InvalidFile }

public sealed record CreateCommentResult(
    CreateCommentStatus Status, CommentDto? Comment = null, string? Error = null);

public interface ICommentService
{
    Task<CreateCommentResult> CreateAsync(
        CreateCommentRequest request, UploadedFile? file, ClientInfo client, CancellationToken ct);
}