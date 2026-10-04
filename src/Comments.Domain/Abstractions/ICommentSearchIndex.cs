namespace Comments.Domain.Abstractions;

/// <summary>Что попадает в поисковый индекс. E-mail автора здесь сознательно отсутствует.</summary>
public sealed record CommentSearchDocument(
    Guid Id,
    Guid? ParentId,
    string UserName,
    string Text,
    DateTime CreatedAt);

public interface ICommentSearchIndex
{
    /// <summary>Добавляет или заменяет документ (повторная доставка сообщения безвредна).</summary>
    Task IndexAsync(CommentSearchDocument document, CancellationToken ct = default);
}