namespace Comments.Domain.Abstractions;

public interface IFileStorage
{
    Task SaveAsync(string storedFileName, Stream content, CancellationToken ct);

    /// <summary>Возвращает поток файла или null, если файла нет.</summary>
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken ct);

    Task DeleteAsync(string storedFileName, CancellationToken ct);
}