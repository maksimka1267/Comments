using SkiaSharp;

namespace Comments.Infrastructure.Captcha;

public sealed class CaptchaImageRenderer
{
    private const int Width = 180;
    private const int Height = 60;

    private static readonly SKColor[] Palette =
    [
        SKColor.Parse("#1f2a44"), SKColor.Parse("#0b6e4f"), SKColor.Parse("#8c2f39"),
        SKColor.Parse("#3d348b"), SKColor.Parse("#b5541c")
    ];

    private readonly SKTypeface _typeface = LoadTypeface();

    private static SKTypeface LoadTypeface()
    {
        // 1) шрифт, встроенный в сборку (нужен в Docker, где системных шрифтов нет)
        var assembly = typeof(CaptchaImageRenderer).Assembly;
        var resource = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("DejaVuSans-Bold.ttf", StringComparison.OrdinalIgnoreCase));

        if (resource is not null)
        {
            using var stream = assembly.GetManifestResourceStream(resource)!;
            using var ms = new MemoryStream();
            stream.CopyTo(ms);
            return SKTypeface.FromData(SKData.CreateCopy(ms.ToArray()));
        }

        // 2) запасной вариант: системный шрифт
        return SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold) ?? SKTypeface.Default;
    }

    public byte[] Render(string code)
    {
        var random = Random.Shared; // здесь случайность нужна только для визуального шума

        using var bitmap = new SKBitmap(Width, Height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColor.Parse("#f4f5f7"));

        using var paint = new SKPaint { IsAntialias = true };

        // шумовые линии под текстом
        paint.Style = SKPaintStyle.Stroke;
        paint.StrokeWidth = 1.5f;
        for (var i = 0; i < 6; i++)
        {
            paint.Color = Palette[random.Next(Palette.Length)].WithAlpha(115);
            canvas.DrawLine(
                random.Next(Width), random.Next(Height),
                random.Next(Width), random.Next(Height), paint);
        }

        // символы с разным размером, цветом, смещением и наклоном
        paint.Style = SKPaintStyle.Fill;
        var step = (Width - 20f) / code.Length;
        for (var i = 0; i < code.Length; i++)
        {
            using var font = new SKFont(_typeface, random.Next(30, 38));
            paint.Color = Palette[random.Next(Palette.Length)];

            var x = 10 + i * step + random.Next(-2, 3);
            var y = random.Next(38, 50);

            canvas.Save();
            canvas.RotateDegrees(random.Next(-15, 16), x, y);
            canvas.DrawText(code[i].ToString(), x, y, font, paint);
            canvas.Restore();
        }

        // точки сверху
        for (var i = 0; i < 80; i++)
        {
            paint.Color = Palette[random.Next(Palette.Length)].WithAlpha(153);
            canvas.DrawCircle(random.Next(Width), random.Next(Height), 1.2f, paint);
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}