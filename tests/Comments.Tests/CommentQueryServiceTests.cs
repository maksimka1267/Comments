using Comments.Api.Contracts;
using Comments.Api.Services;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Comments.Tests;

public class CommentQueryServiceTests
{
    private static AppDbContext CreateDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static Comment Add(AppDbContext db, string userName, DateTime createdAt, Guid? parentId = null)
    {
        var email = $"{userName}@example.com";
        var user = db.Users.FirstOrDefault(u => u.UserName == userName) ?? new User(userName, email, null);
        var comment = new Comment(user, "text", "127.0.0.1", "test", parentId);

        db.Comments.Add(comment);
        db.Entry(comment).Property(c => c.CreatedAt).CurrentValue = createdAt; // дата задаётся вручную
        db.SaveChanges();
        return comment;
    }

    [Fact]
    public async Task Default_order_is_newest_first()
    {
        using var db = CreateDb();
        Add(db, "old", new DateTime(2022, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Add(db, "new", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var result = await new CommentQueryService(db).GetTopLevelAsync(new GetCommentsQuery(), default);

        Assert.Equal(["new", "old"], result.Items.Select(c => c.UserName));
    }

    [Fact]
    public async Task Sort_by_user_name_ascending()
    {
        using var db = CreateDb();
        Add(db, "charlie", DateTime.UtcNow);
        Add(db, "alice", DateTime.UtcNow);
        Add(db, "bob", DateTime.UtcNow);

        var query = new GetCommentsQuery { SortBy = CommentSortField.UserName, SortDir = SortDirection.Asc };
        var result = await new CommentQueryService(db).GetTopLevelAsync(query, default);

        Assert.Equal(["alice", "bob", "charlie"], result.Items.Select(c => c.UserName));
    }

    [Fact]
    public async Task Replies_are_not_in_top_level_list()
    {
        using var db = CreateDb();
        var parent = Add(db, "alice", DateTime.UtcNow);
        Add(db, "bob", DateTime.UtcNow, parent.Id);

        var result = await new CommentQueryService(db).GetTopLevelAsync(new GetCommentsQuery(), default);

        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task Pages_hold_25_items()
    {
        using var db = CreateDb();
        for (var i = 0; i < 30; i++)
            Add(db, $"user{i}", DateTime.UtcNow.AddMinutes(i));

        var service = new CommentQueryService(db);
        var page1 = await service.GetTopLevelAsync(new GetCommentsQuery { Page = 1 }, default);
        var page2 = await service.GetTopLevelAsync(new GetCommentsQuery { Page = 2 }, default);

        Assert.Equal(25, page1.Items.Count);
        Assert.Equal(5, page2.Items.Count);
        Assert.Equal(30, page1.TotalCount);
        Assert.Equal(2, page1.TotalPages);
    }
}