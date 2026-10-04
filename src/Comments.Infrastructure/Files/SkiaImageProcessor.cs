using Comments.Domain.Abstractions;

using SkiaSharp;

namespace Comments.Infrastructure.Files;

public sealed class SkiaImageProcessor : IImageProcessor
{
    public const int MaxWidth = 320;
    public const int MaxHeight = 240;

    // защита от «бомб»: маленький файл, который распаковывается в гигантскую картинку
    private const long MaxPixels = 25_000_000;

    public ProcessedFile? Process(byte[] content)
    {
        using var stream = new MemoryStream(content);
        using var codec = SKCodec.Create(stream);
        if (codec is null)
            return null;

        var sourceFormat = codec.EncodedFormat;
        if (sourceFormat is not (SKEncodedImageFormat.Jpeg or SKEncodedImageFormat.Png or SKEncodedImageFormat.Gif))
            return null;

        if ((long)codec.Info.Width * codec.Info.Height > MaxPixels)
            return null;

        using var source = SKBitmap.Decode(codec);   // у GIF берётся первый кадр
        if (source is null)
            return null;

        var (width, height) = Fit(source.Width, source.Height);
        var needsResize = width != source.Width || height != source.Height;

        var resized = needsResize
            ? source.Resize(new SKImageInfo(width, height),
                new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear))
            : source;

        if (resized is null)
            return null;

        try
        {
            using var image = SKImage.FromBitmap(resized);

            // Skia не умеет писать GIF, поэтому GIF и PNG сохраняем как PNG
            if (sourceFormat == SKEncodedImageFormat.Jpeg)
            {
                using var jpeg = image.Encode(SKEncodedImageFormat.Jpeg, 90);
                return jpeg is null ? null : new ProcessedFile(jpeg.ToArray(), "image/jpeg", ".jpg");
            }

            using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            return png is null ? null : new ProcessedFile(png.ToArray(), "image/png", ".png");
        }
        finally
        {
            if (!ReferenceEquals(resized, source))
                resized.Dispose();
        }
    }

    /// <summary>Пропорционально вписывает размер в 320x240 (увеличивать не нужно).</summary>
    private static (int Width, int Height) Fit(int width, int height)
    {
        var scale = Math.Min(1.0, Math.Min((double)MaxWidth / width, (double)MaxHeight / height));
        if (scale >= 1.0)
            return (width, height);

        return (Math.Max(1, (int)Math.Round(width * scale)),
                Math.Max(1, (int)Math.Round(height * scale)));
    }
}