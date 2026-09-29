namespace Comments.Domain.Entities;

public class Attachment
{
    private Attachment() { } // для EF Core

    public Attachment(AttachmentKind kind, string originalFileName, string storedFileName,
                      string contentType, long sizeBytes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(storedFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sizeBytes);

        Kind = kind;
        OriginalFileName = originalFileName;
        StoredFileName = storedFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
    }

    public Guid Id { get; private set; }

    public Guid CommentId { get; private set; }
    public Comment Comment { get; private set; } = null!;

    public AttachmentKind Kind { get; private set; }
    public string OriginalFileName { get; private set; } = null!;
    public string StoredFileName { get; private set; } = null!;   // имя в хранилище (guid + расширение)
    public string ContentType { get; private set; } = null!;
    public long SizeBytes { get; private set; }
}