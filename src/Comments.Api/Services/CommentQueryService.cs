using System.Linq.Expressions;

using Comments.Api.Contracts;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Comments.Api.Services;

public sealed class CommentQueryService(AppDbContext db) : ICommentQueryService
{
    public const int PageSize = 25;

    private static readonly Expression<Func<Comment, CommentDto>> ToDto = c =>
    new CommentDto(
        c.Id, c.ParentId, c.User.UserName, c.User.Email, c.User.HomePage, c.Text, c.CreatedAt,
        c.Attachment == null
            ? null
            : new AttachmentDto(
                c.Attachment.Id, c.Attachment.Kind, c.Attachment.OriginalFileName,
                c.Attachment.ContentType, c.Attachment.SizeBytes));
    public async Task<PagedResult<CommentDto>> GetTopLevelAsync(GetCommentsQuery query, CancellationToken ct)
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

        var total = await comments.CountAsync(ct);

        var items = await ordered
            .Skip((query.Page - 1) * PageSize)
            .Take(PageSize)
            .Select(ToDto)
            .ToListAsync(ct);

        await AttachRepliesAsync(items, ct);

        return new PagedResult<CommentDto>(items, query.Page, PageSize, total);
    }

    public async Task<CommentDto?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var root = await db.Comments.AsNoTracking()
            .Where(c => c.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync(ct);

        if (root is null)
            return null;

        await AttachRepliesAsync([root], ct);
        return root;
    }

    /// <summary>Загружает всех потомков переданных комментариев и собирает из них дерево.</summary>
    private async Task AttachRepliesAsync(List<CommentDto> roots, CancellationToken ct)
    {
        var descendants = new List<CommentDto>();
        var frontier = roots.Select(r => r.Id).ToList();

        while (frontier.Count > 0)
        {
            var level = await db.Comments.AsNoTracking()
                .Where(c => c.ParentId != null && frontier.Contains(c.ParentId.Value))
                .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)   // ответы в хронологическом порядке
                .Select(ToDto)
                .ToListAsync(ct);

            descendants.AddRange(level);
            frontier = level.Select(c => c.Id).ToList();
        }

        var byParent = descendants.ToLookup(c => c.ParentId!.Value);
        foreach (var comment in roots.Concat(descendants))
            comment.Replies.AddRange(byParent[comment.Id]);
    }
}