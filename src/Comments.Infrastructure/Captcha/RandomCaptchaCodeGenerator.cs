using System.Security.Cryptography;

namespace Comments.Infrastructure.Captcha;

public interface ICaptchaCodeGenerator
{
    string Generate();
}

public sealed class RandomCaptchaCodeGenerator : ICaptchaCodeGenerator
{
    // без похожих символов: 0/O, 1/I
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int Length = 5;

    public string Generate() => RandomNumberGenerator.GetString(Alphabet, Length);
}