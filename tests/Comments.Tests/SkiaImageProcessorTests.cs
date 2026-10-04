using Comments.Infrastructure.Files;

using SkiaSharp;

namespace Comments.Tests;

public class SkiaImageProcessorTests
{
    private readonly SkiaImageProcessor _sut = new();

    private static byte[] Make(SKEncodedImageFormat format, int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Teal);
        using var image = SKImage.FromBitmap(bitmap);
        return image.Encode(format, 90).ToArray();
    }

    private static (int Width, int Height) SizeOf(byte[] bytes)
    {
        using var bitmap = SKBitmap.Decode(bytes);
        return (bitmap.Width, bitmap.Height);
    }

    [Fact]
    public void Large_png_is_scaled_proportionally()
    {
        var result = _sut.Process(Make(SKEncodedImageFormat.Png, 800, 600));

        Assert.NotNull(result);
        Assert.Equal("image/png", result.ContentType);
        Assert.Equal((320, 240), SizeOf(result.Content));
    }

    [Fact]
    public void Tall_image_keeps_aspect_ratio()
    {
        var result = _sut.Process(Make(SKEncodedImageFormat.Png, 200, 1000));

        Assert.Equal((48, 240), SizeOf(result!.Content));
    }

    [Fact]
    public void Small_image_is_not_enlarged()
    {
        var result = _sut.Process(Make(SKEncodedImageFormat.Png, 100, 50));

        Assert.Equal((100, 50), SizeOf(result!.Content));
    }

    [Fact]
    public void Jpeg_stays_jpeg()
    {
        var result = _sut.Process(Make(SKEncodedImageFormat.Jpeg, 640, 480));

        Assert.Equal("image/jpeg", result!.ContentType);
        Assert.Equal(".jpg", result.Extension);
        Assert.Equal((320, 240), SizeOf(result.Content));
    }

    [Fact]
    public void Gif_is_accepted_and_saved_as_png()
    {
        // минимальный GIF 1x1
        var gif = Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7");

        var result = _sut.Process(gif);

        Assert.NotNull(result);
        Assert.Equal("image/png", result.ContentType);
    }

    [Fact]
    public void Random_bytes_are_rejected() =>
        Assert.Null(_sut.Process([1, 2, 3, 4, 5, 6, 7, 8]));

    [Fact]
    public void Text_disguised_as_image_is_rejected() =>
        Assert.Null(_sut.Process("<svg onload=alert(1)></svg>"u8.ToArray()));
}