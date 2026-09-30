using Comments.Api.Contracts;

namespace Comments.Api.Services;

public sealed record ClientInfo(string IpAddress, string UserAgent);

public enum CreateCommentStatus { Created, ParentNotFound, EmptyText, InvalidCaptcha }
public sealed record CreateCommentResult(CreateCommentStatus Status, CommentDto? Comment = null);

public interface ICommentService
{
    Task<CreateCommentResult> CreateAsync(CreateCommentRequest request, ClientInfo client, CancellationToken ct);
}