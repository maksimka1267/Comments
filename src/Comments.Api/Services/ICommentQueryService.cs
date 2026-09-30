using Comments.Api.Contracts;

namespace Comments.Api.Services;

public interface ICommentQueryService
{
    Task<List<CommentDto>> GetTopLevelAsync(GetCommentsQuery query, CancellationToken ct);
}