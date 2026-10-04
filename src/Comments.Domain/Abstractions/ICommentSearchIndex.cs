namespace Comments.Domain.Abstractions;

/// <summary>Что попадает в поисковый индекс. E-mail автора здесь сознательно отсутствует.</summary>
public sealed record CommentSearchDocument(
    Guid Id,
    Guid? ParentId,
    string UserName,
    string Text,
    DateTime CreatedAt);

/// <summary>Найденный комментарий: короткая выдержка из текста без разметки.</summary>
public sealed record CommentSearchHit(
    Guid Id,
    Guid? ParentId,
    string UserName,
    string Snippet,
    DateTime CreatedAt);

public sealed record CommentSearchPage(IReadOnlyList<CommentSearchHit> Hits, long TotalCount);

/// <summary>Поиск недоступен: Elasticsearch не отвечает или вернул ошибку.</summary>
public sealed class SearchUnavailableException(string message) : Exception(message);

public interface ICommentSearchIndex
{
    /// <summary>Добавляет или заменяет документ (повторная доставка сообщения безвредна).</summary>
    Task IndexAsync(CommentSearchDocument document, CancellationToken ct = default);

    /// <summary>Ищет по тексту и имени автора, лучшие совпадения первыми.</summary>
    Task<CommentSearchPage> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default);
}