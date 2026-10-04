using Comments.Api.Contracts;
using Comments.Api.Services;
using Comments.Domain.Abstractions;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Comments.Tests;

public class CachedCommentQueryServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Comment Add(AppDbContext db, string userName, Guid? parentId = null)
    {
        var user = db.Users.FirstOrDefault(u => u.UserName == userName)
                   ?? new User(userName, $"{userName}@example.com", null);
        var comment = new Comment(user, "text", "127.0.0.1", "test", parentId);

        db.Comments.Add(comment);
        db.SaveChanges();
        return comment;
    }

    private static CachedCommentQueryService Create(AppDbContext db, ICacheService cache) =>
        new(new CommentQueryService(db), cache);

    [Fact]
    public async Task Second_call_is_served_from_cache_until_version_changes()
    {
        using var db = CreateDb();
        var cache = new InMemoryCacheService();
        var sut = Create(db, cache);
        Add(db, "alice");

        var first = await sut.GetTopLevelAsync(new GetCommentsQuery(), default);
        Add(db, "bob");
        var cached = await sut.GetTopLevelAsync(new GetCommentsQuery(), default);

        await cache.BumpVersionAsync(CacheScopes.CommentList, default);
        var fresh = await sut.GetTopLevelAsync(new GetCommentsQuery(), default);

        Assert.Equal(1, first.TotalCount);
        Assert.Equal(1, cached.TotalCount);   // из кэша: бэкенд про bob ещё не знает
        Assert.Equal(2, fresh.TotalCount);    // после смены версии данные перечитаны
    }

    [Fact]
    public async Task Different_sorting_and_pages_are_cached_separately()
    {
        using var db = CreateDb();
        var sut = Create(db, new InMemoryCacheService());
        Add(db, "bob");
        Add(db, "alice");

        var byNameAsc = await sut.GetTopLevelAsync(
            new GetCommentsQuery { SortBy = CommentSortField.UserName, SortDir = SortDirection.Asc }, default);
        var byNameDesc = await sut.GetTopLevelAsync(
            new GetCommentsQuery { SortBy = CommentSortField.UserName, SortDir = SortDirection.Desc }, default);

        Assert.Equal(["alice", "bob"], byNameAsc.Items.Select(c => c.UserName));
        Assert.Equal(["bob", "alice"], byNameDesc.Items.Select(c => c.UserName));
    }

    [Fact]
    public async Task Reply_tree_survives_the_cache_round_trip()
    {
        using var db = CreateDb();
        var cache = new InMemoryCacheService();
        var sut = Create(db, cache);
        var root = Add(db, "alice");
        var reply = Add(db, "bob", root.Id);
        Add(db, "carol", reply.Id);

        await sut.GetTopLevelAsync(new GetCommentsQuery(), default);          // наполняет кэш
        var fromCache = await sut.GetTopLevelAsync(new GetCommentsQuery(), default);

        var top = Assert.Single(fromCache.Items);
        var level1 = Assert.Single(top.Replies);
        Assert.Equal("carol", Assert.Single(level1.Replies).UserName);
    }
}