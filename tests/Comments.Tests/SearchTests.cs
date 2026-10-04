using Comments.Api.Contracts;
using Comments.Api.Controllers;
using Comments.Domain.Abstractions;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;
using Comments.Infrastructure.Search;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Comments.Tests;

public class SearchTests
{
    private sealed class FakeIndex : ICommentSearchIndex
    {
        public List<CommentSearchDocument> Indexed { get; } = [];
        public CommentSearchPage Page { get; set; } = new([], 0);
        public bool Fail { get; set; }
        public string? LastQuery { get; private set; }

        public Task IndexAsync(CommentSearchDocument document, CancellationToken ct = default)
        {
            Indexed.Add(document);
            return Task.CompletedTask;
        }

        public Task<CommentSearchPage> SearchAsync(string query, int page, int pageSize, CancellationToken ct = default)
        {
            if (Fail)
                throw new SearchUnavailableException("down");

            LastQuery = query;
            return Task.FromResult(Page);
        }
    }

    [Fact]
    public async Task Controller_maps_hits_and_trims_the_query()
    {
        var id = Guid.NewGuid();
        var created = new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);
        var index = new FakeIndex
        {
            Page = new CommentSearchPage([new CommentSearchHit(id, null, "Anna", "hello", created)], 30),
        };

        var result = await new SearchController(index)
            .Search(new SearchCommentsQuery { Q = "  hello  ", Page = 2 }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var paged = Assert.IsType<PagedResult<SearchHitDto>>(ok.Value);

        Assert.Equal("hello", index.LastQuery);
        Assert.Equal(2, paged.Page);
        Assert.Equal(30, paged.TotalCount);
        Assert.Equal(2, paged.TotalPages);
        Assert.Equal(new SearchHitDto(id, null, "Anna", "hello", created), paged.Items.Single());
    }

    [Fact]
    public async Task Controller_returns_503_when_search_is_unavailable()
    {
        var result = await new SearchController(new FakeIndex { Fail = true })
            .Search(new SearchCommentsQuery { Q = "x" }, CancellationToken.None);

        var problem = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(503, problem.StatusCode);
    }

    [Fact]
    public void Snippet_cuts_long_text()
    {
        Assert.Equal("short", ElasticCommentSearchIndex.Snippet("short"));

        var snippet = ElasticCommentSearchIndex.Snippet(new string('a', 500));
        Assert.EndsWith("…", snippet);
        Assert.True(snippet.Length <= 201);
    }

    [Fact]
    public async Task Reindexer_sends_every_comment_in_batches_without_markup_or_email()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var db = new AppDbContext(options);

        var user = new User("Anna123", "anna@example.com", null);
        var first = new Comment(user, "<strong>hello</strong>", "127.0.0.1", "test");
        db.Add(first);
        for (var i = 0; i < 4; i++)
            db.Add(new Comment(user, $"reply {i}", "127.0.0.1", "test", first.Id));
        await db.SaveChangesAsync();

        var index = new FakeIndex();

        // размер пачки 2 при 5 комментариях: проверяем, что обходятся все пачки
        var count = await new CommentSearchReindexer(db, index).ReindexAsync(CancellationToken.None, batchSize: 2);

        Assert.Equal(5, count);
        Assert.Equal(5, index.Indexed.Select(d => d.Id).Distinct().Count());
        Assert.Contains(index.Indexed, d => d.Text == "hello");
        Assert.All(index.Indexed, d => Assert.Equal("Anna123", d.UserName));
    }
}