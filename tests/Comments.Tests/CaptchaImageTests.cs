using Comments.Infrastructure.Captcha;

namespace Comments.Tests;

public class CaptchaImageTests
{
    [Fact]
    public void Renderer_returns_png()
    {
        var bytes = new CaptchaImageRenderer().Render("AB3CD");

        Assert.True(bytes.Length > 500);
        Assert.Equal(new byte[] { 0x89, 0x50, 0x4E, 0x47 }, bytes[..4]); // сигнатура PNG
    }

    [Fact]
    public void Generator_returns_five_safe_characters()
    {
        var generator = new RandomCaptchaCodeGenerator();

        for (var i = 0; i < 100; i++)
            Assert.Matches("^[A-HJ-NP-Z2-9]{5}$", generator.Generate());
    }
}