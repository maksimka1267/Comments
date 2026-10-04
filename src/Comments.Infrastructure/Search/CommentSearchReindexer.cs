using Comments.Domain.Abstractions;
using Comments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Comments.Infrastructure.Search;

/// <summary>Перестраивает поисковый индекс из базы данных (комментарии старше очереди и потерянные события).</summary>
public sealed class CommentSearchReindexer(AppDbContext db, ICommentSearchIndex index)
{
    public const int DefaultBatchSize = 200;

    /// <summary>Индексирует все комментарии; повторный запуск безвреден, документы заменяются по id.</summary>
    public async Task<int> ReindexAsync(CancellationToken ct, int batchSize = DefaultBatchSize)
    {
        var total = 0;

        while (true)
        {
            var batch = await db.Comments.AsNoTracking()
                .OrderBy(c => c.CreatedAt).ThenBy(c => c.Id)
                .Skip(total)
                .Take(batchSize)
                .Select(c => new { c.Id, c.ParentId, c.User.UserName, c.Text, c.CreatedAt })
                .ToListAsync(ct);

            if (batch.Count == 0)
                return total;

            foreach (var c in batch)
            {
                await index.IndexAsync(
                    new CommentSearchDocument(c.Id, c.ParentId, c.UserName, PlainText.FromHtml(c.Text), c.CreatedAt),
                    ct);
            }

            total += batch.Count;
        }
    }
}