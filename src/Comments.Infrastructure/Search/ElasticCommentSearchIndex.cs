using Comments.Domain.Abstractions;

using Elastic.Clients.Elasticsearch;

namespace Comments.Infrastructure.Search;

public sealed class ElasticCommentSearchIndex(ElasticsearchClient client) : ICommentSearchIndex
{
    public const string IndexName = "comments";

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
            throw new InvalidOperationException(
                $"Failed to index comment {document.Id}: {response.DebugInformation}");
    }

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
                        throw new InvalidOperationException(
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