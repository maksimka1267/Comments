using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Comments.Infrastructure.Captcha;

public sealed class CaptchaImageRenderer
{
    private const int Width = 180;
    private const int Height = 60;

    private static readonly Color[] Palette =
    [
        Color.ParseHex("#1f2a44"), Color.ParseHex("#0b6e4f"), Color.ParseHex("#8c2f39"),
        Color.ParseHex("#3d348b"), Color.ParseHex("#b5541c")
    ];

    private readonly FontFamily _fontFamily;

    public CaptchaImageRenderer()
    {
        _fontFamily = SystemFonts.TryGet("Arial", out var family)
            ? family
            : SystemFonts.Families.First();
    }

    public byte[] Render(string code)
    {
        var random = Random.Shared; // здесь случайность нужна только для визуального шума

        using var image = new Image<Rgba32>(Width, Height);
        image.Mutate(ctx =>
        {
            ctx.Fill(Color.ParseHex("#f4f5f7"));

            // шумовые линии под текстом
            for (var i = 0; i < 6; i++)
            {
                ctx.DrawLine(
                    Palette[random.Next(Palette.Length)].WithAlpha(0.45f), 1.5f,
                    new PointF(random.Next(Width), random.Next(Height)),
                    new PointF(random.Next(Width), random.Next(Height)));
            }

            // символы с разным размером, цветом и смещением
            var step = (Width - 20f) / code.Length;
            for (var i = 0; i < code.Length; i++)
            {
                var font = _fontFamily.CreateFont(random.Next(30, 38));
                var position = new PointF(10 + i * step + random.Next(-2, 3), random.Next(6, 14));
                ctx.DrawText(code[i].ToString(), font, Palette[random.Next(Palette.Length)], position);
            }

            // точки сверху
            for (var i = 0; i < 80; i++)
            {
                ctx.Fill(Palette[random.Next(Palette.Length)].WithAlpha(0.6f),
                    new EllipsePolygon(random.Next(Width), random.Next(Height), 1.2f));
            }
        });

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}