using Comments.Domain.Abstractions;

using Elastic.Clients.Elasticsearch;

namespace Comments.Infrastructure.Search;

public sealed class ElasticCommentSearchIndex(ElasticsearchClient client) : ICommentSearchIndex
{
    public const string IndexName = "comments";
    private const int SnippetLength = 200;

    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _ready;

    public async Task IndexAsync(CommentSearchDocument document, CancellationToken ct = default)
    {
        await EnsureIndexAsync(ct);

        var response = await client.IndexAsync(
            CommentIndexDocument.From(document),
            i => i.Index(IndexName).Id(document.Id.ToString()),
            ct);

        if (!response.IsValidResponse)
            throw new SearchUnavailableException(
                $"Failed to index comment {document.Id}: {response.DebugInformation}");
    }

    public async Task<CommentSearchPage> SearchAsync(
        string query, int page, int pageSize, CancellationToken ct = default)
    {
        // индекса может ещё не быть, если не создан ни один комментарий
        await EnsureIndexAsync(ct);

        // match принимает обычный текст, а не синтаксис запросов, поэтому спецсимволы безопасны
        var response = await client.SearchAsync<CommentIndexDocument>(s => s
            .Indices(IndexName)
            .From((page - 1) * pageSize)
            .Size(pageSize)
            .Query(q => q.Bool(b => b.Should(
                should => should.Match(m => m.Field(d => d.Text).Query(query)),
                should => should.Match(m => m.Field(d => d.UserName).Query(query))))),
            ct);

        if (!response.IsValidResponse)
            throw new SearchUnavailableException($"Search failed: {response.DebugInformation}");

        var hits = response.Documents
            .Select(d => new CommentSearchHit(d.Id, d.ParentId, d.UserName, Snippet(d.Text), d.CreatedAt))
            .ToList();

        return new CommentSearchPage(hits, response.Total);
    }

    public static string Snippet(string text) =>
        text.Length <= SnippetLength ? text : text[..SnippetLength].TrimEnd() + "…";

    /// <summary>Создаёт индекс при первом обращении; Elasticsearch мог ещё не запуститься при старте API.</summary>
    private async Task EnsureIndexAsync(CancellationToken ct)
    {
        if (_ready)
            return;

        await _lock.WaitAsync(ct);
        try
        {
            if (_ready)
                return;

            var exists = await client.Indices.ExistsAsync(IndexName, ct);
            if (!exists.Exists)
            {
                var created = await client.Indices.CreateAsync<CommentIndexDocument>(
                    IndexName,
                    c => c.Mappings(m => m.Properties(p => p
                        .Keyword(d => d.Id)
                        .Keyword(d => d.ParentId)
                        .Text(d => d.UserName)
                        // русский анализатор: морфология для кириллицы, латиница ищется как обычные слова
                        .Text(d => d.Text, t => t.Analyzer("russian"))
                        .Date(d => d.CreatedAt))),
                    ct);

                if (!created.IsValidResponse)
                {
                    // индекс мог создать другой экземпляр API одновременно с нами
                    var again = await client.Indices.ExistsAsync(IndexName, ct);
                    if (!again.Exists)
                        throw new SearchUnavailableException(
                            $"Could not create index '{IndexName}': {created.DebugInformation}");
                }
            }

            _ready = true;
        }
        finally
        {
            _lock.Release();
        }
    }
}