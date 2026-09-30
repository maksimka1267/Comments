using Comments.Infrastructure.Files;

using Microsoft.Extensions.Options;

namespace Comments.Tests;

public sealed class LocalFileStorageTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly LocalFileStorage _sut;

    public LocalFileStorageTests() =>
        _sut = new LocalFileStorage(Options.Create(new FileStorageOptions { RootPath = _root }));

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public async Task Saved_file_can_be_read_back()
    {
        await _sut.SaveAsync("a.txt", new MemoryStream("hello"u8.ToArray()), default);

        await using var stream = await _sut.OpenReadAsync("a.txt", default);
        using var reader = new StreamReader(stream!);

        Assert.Equal("hello", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Missing_file_returns_null() =>
        Assert.Null(await _sut.OpenReadAsync("nope.txt", default));

    [Fact]
    public async Task Deleted_file_is_gone()
    {
        await _sut.SaveAsync("a.txt", new MemoryStream([1, 2, 3]), default);

        await _sut.DeleteAsync("a.txt", default);

        Assert.Null(await _sut.OpenReadAsync("a.txt", default));
    }

    [Theory]
    [InlineData("../evil.txt")]
    [InlineData("..\\evil.txt")]
    [InlineData("sub/evil.txt")]
    public async Task Path_traversal_is_rejected(string name) =>
        await Assert.ThrowsAsync<ArgumentException>(
            () => _sut.SaveAsync(name, new MemoryStream([1]), default));
}