using System.Text.Json;

using Comments.Api.Contracts;
using Comments.Api.GraphQL;
using Comments.Api.Services;

using HotChocolate;
using HotChocolate.Execution;

using Microsoft.Extensions.DependencyInjection;

namespace Comments.Tests;

public class GraphQlSchemaTests
{
    private sealed class FakeQueries : ICommentQueryService
    {
        public GetCommentsQuery? LastQuery { get; private set; }
        public int Calls { get; private set; }

        public Task<PagedResult<CommentDto>> GetTopLevelAsync(GetCommentsQuery query, CancellationToken ct)
        {
            LastQuery = query;
            Calls++;

            var reply = new CommentDto(
                Guid.NewGuid(), Guid.NewGuid(), "Bob", "bob@example.com", null, "reply", DateTime.UtcNow, null);
            var top = new CommentDto(
                Guid.NewGuid(), null, "Anna", "anna@example.com", null, "top", DateTime.UtcNow, null)
            {
                Replies = [reply],
            };

            return Task.FromResult(new PagedResult<CommentDto>([top], query.Page, 25, 30));
        }

        public Task<CommentDto?> GetByIdAsync(Guid id, CancellationToken ct) =>
            Task.FromResult<CommentDto?>(null);
    }

    private static async Task<(IRequestExecutor Executor, FakeQueries Queries)> CreateAsync()
    {
        var queries = new FakeQueries();

        var executor = await new ServiceCollection()
            .AddSingleton<ICommentQueryService>(queries)
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddMaxExecutionDepthRule(12)
            .BuildRequestExecutorAsync();

        return (executor, queries);
    }

    private static async Task<JsonElement> RunAsync(IRequestExecutor executor, string query)
    {
        var result = await executor.ExecuteAsync(query);
        return JsonDocument.Parse(result.ToJson()).RootElement.Clone();
    }

    [Fact]
    public async Task Returns_page_with_nested_replies_and_default_sorting()
    {
        var (executor, queries) = await CreateAsync();

        var json = await RunAsync(executor,
            "{ comments { page totalCount totalPages items { userName replies { userName } } } }");

        var comments = json.GetProperty("data").GetProperty("comments");
        Assert.Equal(30, comments.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, comments.GetProperty("totalPages").GetInt32());

        var top = comments.GetProperty("items")[0];
        Assert.Equal("Anna", top.GetProperty("userName").GetString());
        Assert.Equal("Bob", top.GetProperty("replies")[0].GetProperty("userName").GetString());

        Assert.Equal(CommentSortField.Date, queries.LastQuery!.SortBy);
        Assert.Equal(SortDirection.Desc, queries.LastQuery.SortDir);
        Assert.Equal(1, queries.LastQuery.Page);
    }

    [Fact]
    public async Task Passes_sorting_arguments_to_the_query_service()
    {
        var (executor, queries) = await CreateAsync();

        await RunAsync(executor, "{ comments(sortBy: USER_NAME, sortDir: ASC, page: 2) { page } }");

        Assert.Equal(CommentSortField.UserName, queries.LastQuery!.SortBy);
        Assert.Equal(SortDirection.Asc, queries.LastQuery.SortDir);
        Assert.Equal(2, queries.LastQuery.Page);
    }

    [Fact]
    public async Task Rejects_page_below_one_without_calling_the_service()
    {
        var (executor, queries) = await CreateAsync();

        var json = await RunAsync(executor, "{ comments(page: 0) { page } }");

        Assert.Equal(
            "INVALID_PAGE",
            json.GetProperty("errors")[0].GetProperty("extensions").GetProperty("code").GetString());
        Assert.Equal(0, queries.Calls);
    }

    [Fact]
    public async Task Returns_null_for_unknown_comment()
    {
        var (executor, _) = await CreateAsync();

        var json = await RunAsync(executor, $"{{ comment(id: \"{Guid.NewGuid()}\") {{ id }} }}");

        Assert.Equal(JsonValueKind.Null, json.GetProperty("data").GetProperty("comment").ValueKind);
    }

    [Fact]
    public async Task Rejects_overly_deep_queries()
    {
        var (executor, queries) = await CreateAsync();

        const int depth = 20;
        var nested = string.Concat(Enumerable.Repeat("replies { ", depth)) + "id" + new string('}', depth);

        var json = await RunAsync(executor, $"{{ comments {{ items {{ {nested} }} }} }}");

        Assert.True(json.TryGetProperty("errors", out _));
        Assert.Equal(0, queries.Calls);
    }
}