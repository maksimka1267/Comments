using Comments.Api.Contracts;
using Comments.Api.Services;
using Comments.Domain.Abstractions;
using Comments.Domain.Entities;
using Comments.Domain.Events;
using Comments.Infrastructure.Files;
using Comments.Infrastructure.Persistence;
using Comments.Infrastructure.Text;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using SkiaSharp;

namespace Comments.Tests;

public sealed class CommentServiceAttachmentTests : IDisposable
{
    private sealed class FakeCaptcha(bool result) : ICaptchaService
    {
        public Task<CaptchaChallenge> CreateAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<bool> VerifyAsync(Guid id, string? answer, CancellationToken ct) => Task.FromResult(result);
    }

    private static readonly ClientInfo Client = new("127.0.0.1", "test");

    private readonly string _root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly AppDbContext _db = new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private readonly LocalFileStorage _storage;

    public CommentServiceAttachmentTests() =>
        _storage = new LocalFileStorage(Options.Create(new FileStorageOptions { RootPath = _root }));

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    private sealed class RecordingDispatcher : IEventDispatcher
    {
        public List<object> Events { get; } = [];

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct)
        {
            Events.Add(@event!);
            return Task.CompletedTask;
        }
    }

    private CommentService CreateService(bool captchaOk = true, IEventDispatcher? events = null) => new(
        _db, new HtmlMessageSanitizer(), new FakeCaptcha(captchaOk),
        new SkiaImageProcessor(), new TextFileProcessor(), _storage,
        events ?? new RecordingDispatcher());

    [Fact]
    public async Task Creating_a_comment_publishes_an_event()
    {
        var events = new RecordingDispatcher();

        await CreateService(events: events).CreateAsync(Request(), null, Client, default);

        var published = Assert.IsType<CommentCreatedEvent>(Assert.Single(events.Events));
        Assert.Equal("Anna", published.UserName);
    }

    [Fact]
    public async Task Rejected_comment_publishes_nothing()
    {
        var events = new RecordingDispatcher();

        await CreateService(captchaOk: false, events: events).CreateAsync(Request(), null, Client, default);

        Assert.Empty(events.Events);
    }

    private static CreateCommentRequest Request() =>
        new("Anna", "anna@example.com", null, "Hello", null, Guid.NewGuid(), "AB3CD");

    private static byte[] Png(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(SKEncodedImageFormat.Png, 100).ToArray();
    }

    [Fact]
    public async Task Image_is_processed_and_stored()
    {
        var result = await CreateService().CreateAsync(
            Request(), new UploadedFile("photo.png", Png(800, 600)), Client, default);

        Assert.Equal(CreateCommentStatus.Created, result.Status);
        Assert.Equal(AttachmentKind.Image, result.Comment!.Attachment!.Kind);

        var stored = _db.Attachments.Single().StoredFileName;
        await using var stream = await _storage.OpenReadAsync(stored, default);
        Assert.NotNull(stream);
    }

    [Fact]
    public async Task Text_file_is_stored()
    {
        var result = await CreateService().CreateAsync(
            Request(), new UploadedFile("notes.txt", "hello"u8.ToArray()), Client, default);

        Assert.Equal(CreateCommentStatus.Created, result.Status);
        Assert.Equal(AttachmentKind.Text, result.Comment!.Attachment!.Kind);
        Assert.Equal("notes.txt", result.Comment.Attachment.FileName);
    }

    [Fact]
    public async Task Comment_without_file_has_no_attachment()
    {
        var result = await CreateService().CreateAsync(Request(), null, Client, default);

        Assert.Equal(CreateCommentStatus.Created, result.Status);
        Assert.Null(result.Comment!.Attachment);
    }

    [Theory]
    [InlineData("virus.exe")]
    [InlineData("page.html")]
    public async Task Unsupported_extension_is_rejected_and_nothing_is_saved(string fileName)
    {
        var result = await CreateService().CreateAsync(
            Request(), new UploadedFile(fileName, [1, 2, 3]), Client, default);

        Assert.Equal(CreateCommentStatus.InvalidFile, result.Status);
        Assert.Empty(_db.Comments);
        Assert.Empty(Directory.GetFiles(_root));
    }

    [Fact]
    public async Task Fake_image_is_rejected()
    {
        var result = await CreateService().CreateAsync(
            Request(), new UploadedFile("photo.png", "not an image"u8.ToArray()), Client, default);

        Assert.Equal(CreateCommentStatus.InvalidFile, result.Status);
        Assert.Empty(_db.Comments);
    }

    [Fact]
    public async Task Wrong_captcha_is_rejected()
    {
        var result = await CreateService(captchaOk: false).CreateAsync(Request(), null, Client, default);

        Assert.Equal(CreateCommentStatus.InvalidCaptcha, result.Status);
        Assert.Empty(_db.Comments);
    }
}