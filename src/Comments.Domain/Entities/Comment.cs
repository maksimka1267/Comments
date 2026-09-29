namespace Comments.Domain.Entities;

public class Comment
{
    private Comment() { } // для EF Core

    public Comment(User user, string text, string ipAddress, string userAgent, long? parentId = null)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        ArgumentException.ThrowIfNullOrWhiteSpace(ipAddress);

        User = user;
        Text = text;
        IpAddress = ipAddress;
        UserAgent = userAgent ?? string.Empty;
        ParentId = parentId;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid? ParentId { get; private set; }
    public Comment? Parent { get; private set; }
    public ICollection<Comment> Replies { get; private set; } = new List<Comment>();

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    /// <summary>Уже очищенный (sanitized) текст.</summary>
    public string Text { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }

    public string IpAddress { get; private set; } = null!;
    public string UserAgent { get; private set; } = null!;
    public Attachment? Attachment { get; private set; }

    public void AttachFile(Attachment attachment)
    {
        ArgumentNullException.ThrowIfNull(attachment);
        if (Attachment is not null)
            throw new InvalidOperationException("Comment already has an attachment.");

        Attachment = attachment;
    }
}