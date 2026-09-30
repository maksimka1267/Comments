namespace Comments.Domain.Abstractions;

public sealed record ProcessedFile(byte[] Content, string ContentType, string Extension);

public interface IImageProcessor
{
    /// <summary>Проверяет и перекодирует картинку (JPG/GIF/PNG), уменьшая до 320x240. Null, если файл не подходит.</summary>
    ProcessedFile? Process(byte[] content);
}

public interface ITextFileProcessor
{
    /// <summary>Проверяет TXT-файл (до 100 КБ). Null, если файл не подходит.</summary>
    ProcessedFile? Process(byte[] content);
}