using Comments.Api.Contracts;
using Comments.Domain.Abstractions;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Comments.Api.Services;

public sealed class CommentService(AppDbContext db, IMessageSanitizer sanitizer) : ICommentService
{
    private const int MaxUserAgentLength = 512;

    public async Task<CreateCommentResult> CreateAsync(
        CreateCommentRequest request, ClientInfo client, CancellationToken ct)
    {
        if (request.ParentId is { } parentId &&
            !await db.Comments.AnyAsync(c => c.Id == parentId, ct))
            return new CreateCommentResult(CreateCommentStatus.ParentNotFound);

        // в базу попадает только очищенный текст
        var text = sanitizer.Sanitize(request.Text.Trim());
        if (string.IsNullOrWhiteSpace(text))
            return new CreateCommentResult(CreateCommentStatus.EmptyText);

        var userName = request.UserName.Trim();
        var email = request.Email.Trim();
        var homePage = request.HomePage?.Trim();

        var user = await db.Users.FirstOrDefaultAsync(u => u.UserName == userName && u.Email == email, ct);
        if (user is null)
        {
            user = new User(userName, email, homePage);
            db.Users.Add(user);
        }
        else if (!string.IsNullOrWhiteSpace(homePage))
        {
            user.UpdateHomePage(homePage);
        }

        var userAgent = client.UserAgent.Length > MaxUserAgentLength
            ? client.UserAgent[..MaxUserAgentLength]
            : client.UserAgent;

        var comment = new Comment(user, text, client.IpAddress, userAgent, request.ParentId);
        db.Comments.Add(comment);
        await db.SaveChangesAsync(ct);

        return new CreateCommentResult(
            CreateCommentStatus.Created,
            new CommentDto(comment.Id, comment.ParentId, user.UserName, user.Email,
                           user.HomePage, comment.Text, comment.CreatedAt));
    }
}