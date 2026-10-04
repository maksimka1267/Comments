using Comments.Api.Contracts;
using Comments.Domain.Abstractions;
using Comments.Domain.Entities;
using Comments.Domain.Events;
using Comments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

using Path = System.IO.Path;

namespace Comments.Api.Services;

public sealed class CommentService(
    AppDbContext db,
    IMessageSanitizer sanitizer,
    ICaptchaService captcha,
    IImageProcessor images,
    ITextFileProcessor texts,
    IFileStorage storage,
    IEventDispatcher events) : ICommentService
{
    private const int MaxUserAgentLength = 512;
    private const int MaxFileNameLength = 255;

    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png", ".gif"];

    private sealed record PreparedFile(AttachmentKind Kind, ProcessedFile? Content, string OriginalName, string? Error);

    public async Task<CreateCommentResult> CreateAsync(
        CreateCommentRequest request, UploadedFile? file, ClientInfo client, CancellationToken ct)
    {
        // сначала дешёвая защита: без верной капчи тяжёлую обработку файла не запускаем
        if (!await captcha.VerifyAsync(request.CaptchaId, request.CaptchaAnswer, ct))
            return new CreateCommentResult(CreateCommentStatus.InvalidCaptcha);

        if (request.ParentId is { } parentId &&
            !await db.Comments.AnyAsync(c => c.Id == parentId, ct))
            return new CreateCommentResult(CreateCommentStatus.ParentNotFound);

        // в базу попадает только очищенный текст
        var text = sanitizer.Sanitize(request.Text.Trim());
        if (string.IsNullOrWhiteSpace(text))
            return new CreateCommentResult(CreateCommentStatus.EmptyText);

        var prepared = file is null ? null : PrepareAttachment(file);
        if (prepared is { Error: not null })
            return new CreateCommentResult(CreateCommentStatus.InvalidFile, Error: prepared.Error);

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

        string? storedName = null;
        if (prepared?.Content is { } processed)
        {
            storedName = $"{Guid.NewGuid():N}{processed.Extension}";
            await storage.SaveAsync(storedName, new MemoryStream(processed.Content), ct);

            comment.AttachFile(new Attachment(
                prepared.Kind, prepared.OriginalName, storedName, processed.ContentType, processed.Content.Length));
        }

        db.Comments.Add(comment);
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch
        {
            // не оставляем «осиротевший» файл, если запись в БД не удалась
            if (storedName is not null)
                await storage.DeleteAsync(storedName, CancellationToken.None);
            throw;
        }

        // комментарий сохранён: сообщаем об этом обработчикам (сброс кэша, публикация в очередь)
        await events.PublishAsync(
            new CommentCreatedEvent(comment.Id, comment.ParentId, user.UserName, user.Email,
                                    comment.Text, comment.CreatedAt),
            ct);

        var attachment = comment.Attachment is null
            ? null
            : new AttachmentDto(comment.Attachment.Id, comment.Attachment.Kind,
                comment.Attachment.OriginalFileName, comment.Attachment.ContentType, comment.Attachment.SizeBytes);

        return new CreateCommentResult(
            CreateCommentStatus.Created,
            new CommentDto(comment.Id, comment.ParentId, user.UserName, user.Email,
                           user.HomePage, comment.Text, comment.CreatedAt, attachment));
    }

    private PreparedFile PrepareAttachment(UploadedFile upload)
    {
        var name = Path.GetFileName(upload.FileName);
        if (string.IsNullOrWhiteSpace(name))
            name = "file";
        else if (name.Length > MaxFileNameLength)
            name = name[..MaxFileNameLength];

        var extension = Path.GetExtension(name).ToLowerInvariant();

        if (ImageExtensions.Contains(extension))
        {
            var image = images.Process(upload.Content);
            return new PreparedFile(AttachmentKind.Image, image, name,
                image is null ? "Image must be a valid JPG, GIF or PNG file." : null);
        }

        if (extension == ".txt")
        {
            var text = texts.Process(upload.Content);
            return new PreparedFile(AttachmentKind.Text, text, name,
                text is null ? "Text file must be a non-empty TXT file up to 100 KB." : null);
        }

        return new PreparedFile(AttachmentKind.Image, null, name, "Only JPG, GIF, PNG and TXT files are allowed.");
    }
}