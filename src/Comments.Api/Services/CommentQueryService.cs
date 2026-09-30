using Comments.Api.Contracts;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Comments.Api.Services;

public sealed class CommentQueryService(AppDbContext db) : ICommentQueryService
{
    public async Task<List<CommentDto>> GetTopLevelAsync(GetCommentsQuery query, CancellationToken ct)
    {
        var comments = db.Comments.AsNoTracking().Where(c => c.ParentId == null);

        // Id в конце нужен для стабильного порядка при одинаковых значениях
        IOrderedQueryable<Comment> ordered = (query.SortBy, query.SortDir) switch
        {
            (CommentSortField.UserName, SortDirection.Asc) => comments.OrderBy(c => c.User.UserName).ThenBy(c => c.Id),
            (CommentSortField.UserName, SortDirection.Desc) => comments.OrderByDescending(c => c.User.UserName).ThenByDescending(c => c.Id),
            (CommentSortField.Email, SortDirection.Asc) => comments.OrderBy(c => c.User.Email).ThenBy(c => c.Id),
            (CommentSortField.Email, SortDirection.Desc) => comments.OrderByDescending(c => c.User.Email).ThenByDescending(c => c.Id),
            (_, SortDirection.Asc) => comments.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
            _ => comments.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
        };

        return await ordered
            .Select(c => new CommentDto(
                c.Id, c.ParentId, c.User.UserName, c.User.Email, c.User.HomePage, c.Text, c.CreatedAt))
            .ToListAsync(ct);
    }
}