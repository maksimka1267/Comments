using Comments.Domain.Abstractions;

using Microsoft.Extensions.Options;

namespace Comments.Infrastructure.Files;

public sealed class FileStorageOptions
{
    public string RootPath { get; set; } = "uploads";
}

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IOptions<FileStorageOptions> options)
    {
        _root = Path.GetFullPath(options.Value.RootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string storedFileName, Stream content, CancellationToken ct)
    {
        await using var file = File.Create(ResolvePath(storedFileName));
        await content.CopyToAsync(file, ct);
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken ct)
    {
        var path = ResolvePath(storedFileName);

        Stream? stream = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true)
            : null;

        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken ct)
    {
        var path = ResolvePath(storedFileName);
        if (File.Exists(path))
            File.Delete(path);

        return Task.CompletedTask;
    }

    // защита от path traversal: допускаем только голое имя файла без каталогов
    private string ResolvePath(string storedFileName)
    {
        if (string.IsNullOrWhiteSpace(storedFileName) ||
            Path.GetFileName(storedFileName) != storedFileName)
            throw new ArgumentException("Invalid file name.", nameof(storedFileName));

        return Path.Combine(_root, storedFileName);
    }
}