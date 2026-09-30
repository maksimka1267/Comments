using Comments.Domain.Abstractions;
using Comments.Domain.Entities;
using Comments.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;

namespace Comments.Api.Services;

public sealed record AttachmentFile(Stream Content, string ContentType, AttachmentKind Kind);

public interface IAttachmentService
{
    Task<AttachmentFile?> GetAsync(Guid id, CancellationToken ct);
}

public sealed class AttachmentService(AppDbContext db, IFileStorage storage) : IAttachmentService
{
    public async Task<AttachmentFile?> GetAsync(Guid id, CancellationToken ct)
    {
        var attachment = await db.Attachments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);
        if (attachment is null)
            return null;

        var stream = await storage.OpenReadAsync(attachment.StoredFileName, ct);
        return stream is null ? null : new AttachmentFile(stream, attachment.ContentType, attachment.Kind);
    }
}