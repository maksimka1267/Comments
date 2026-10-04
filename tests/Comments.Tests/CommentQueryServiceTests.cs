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
    [Fact]
    public async Task Replies_are_returned_as_a_tree()
    {
        using var db = CreateDb();
        var root = Add(db, "alice", DateTime.UtcNow);
        var reply = Add(db, "bob", DateTime.UtcNow, root.Id);
        Add(db, "carol", DateTime.UtcNow, reply.Id);

        var result = await new CommentQueryService(db).GetTopLevelAsync(new GetCommentsQuery(), default);

        var top = Assert.Single(result.Items);
        var level1 = Assert.Single(top.Replies);
        Assert.Equal("bob", level1.UserName);
        var level2 = Assert.Single(level1.Replies);
        Assert.Equal("carol", level2.UserName);
    }

    [Fact]
    public async Task Replies_are_in_chronological_order()
    {
        using var db = CreateDb();
        var t = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var root = Add(db, "alice", t);
        Add(db, "second", t.AddMinutes(2), root.Id);
        Add(db, "first", t.AddMinutes(1), root.Id);

        var dto = await new CommentQueryService(db).GetByIdAsync(root.Id, default);

        Assert.Equal(["first", "second"], dto!.Replies.Select(r => r.UserName));
    }

    [Fact]
    public async Task GetById_returns_null_for_unknown_comment()
    {
        using var db = CreateDb();

        var dto = await new CommentQueryService(db).GetByIdAsync(Guid.NewGuid(), default);

        Assert.Null(dto);
    }
    [Fact]
    public async Task Attachment_is_included_in_comment()
    {
        using var db = CreateDb();
        var comment = Add(db, "alice", DateTime.UtcNow);
        comment.AttachFile(new Attachment(AttachmentKind.Text, "notes.txt", "x.txt", "text/plain", 10));
        db.SaveChanges();

        var dto = await new CommentQueryService(db).GetByIdAsync(comment.Id, default);

        Assert.Equal("notes.txt", dto!.Attachment!.FileName);
        Assert.Equal(AttachmentKind.Text, dto.Attachment.Kind);
    }
}