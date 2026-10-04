using Comments.Domain.Abstractions;

namespace Comments.Infrastructure.Files;

public sealed class TextFileProcessor : ITextFileProcessor
{
    public const int MaxBytes = 100 * 1024;

    public ProcessedFile? Process(byte[] content)
    {
        if (content.Length == 0 || content.Length > MaxBytes)
            return null;

        // нулевой байт есть у бинарных файлов, в обычном тексте его не бывает
        if (Array.IndexOf(content, (byte)0) >= 0)
            return null;

        return new ProcessedFile(content, "text/plain", ".txt");
    }
}