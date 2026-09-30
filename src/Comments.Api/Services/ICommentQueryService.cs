using Comments.Api.Contracts;

namespace Comments.Api.Services;

public interface ICommentQueryService
{
    Task<PagedResult<CommentDto>> GetTopLevelAsync(GetCommentsQuery query, CancellationToken ct);
}