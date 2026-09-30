using Comments.Infrastructure.Files;

namespace Comments.Tests;

public class TextFileProcessorTests
{
    private readonly TextFileProcessor _sut = new();

    [Fact]
    public void Plain_text_is_accepted()
    {
        var result = _sut.Process("Привет, мир"u8.ToArray());

        Assert.NotNull(result);
        Assert.Equal("text/plain", result.ContentType);
        Assert.Equal(".txt", result.Extension);
    }

    [Fact]
    public void Exactly_100_kb_is_accepted() =>
        Assert.NotNull(_sut.Process(new byte[TextFileProcessor.MaxBytes].Select(_ => (byte)'a').ToArray()));

    [Fact]
    public void More_than_100_kb_is_rejected() =>
        Assert.Null(_sut.Process(new byte[TextFileProcessor.MaxBytes + 1].Select(_ => (byte)'a').ToArray()));

    [Fact]
    public void Empty_file_is_rejected() =>
        Assert.Null(_sut.Process([]));

    [Fact]
    public void Binary_file_is_rejected() =>
        Assert.Null(_sut.Process([0x4D, 0x5A, 0x00, 0x03]));
}