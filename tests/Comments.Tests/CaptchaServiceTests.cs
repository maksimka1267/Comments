using Comments.Infrastructure.Captcha;

using Microsoft.Extensions.Caching.Memory;

namespace Comments.Tests;

public class CaptchaServiceTests
{
    private sealed class FixedCodeGenerator(string code) : ICaptchaCodeGenerator
    {
        public string Generate() => code;
    }

    private static CaptchaService Create() => new(
        new FixedCodeGenerator("AB3CD"),
        new CaptchaImageRenderer(),
        new MemoryCaptchaStore(new MemoryCache(new MemoryCacheOptions())));

    [Fact]
    public async Task Correct_answer_passes_ignoring_case()
    {
        var sut = Create();
        var challenge = await sut.CreateAsync(default);

        Assert.True(await sut.VerifyAsync(challenge.Id, "ab3cd", default));
    }

    [Fact]
    public async Task Wrong_answer_fails()
    {
        var sut = Create();
        var challenge = await sut.CreateAsync(default);

        Assert.False(await sut.VerifyAsync(challenge.Id, "XXXXX", default));
    }

    [Fact]
    public async Task Captcha_is_one_time()
    {
        var sut = Create();
        var challenge = await sut.CreateAsync(default);

        Assert.True(await sut.VerifyAsync(challenge.Id, "AB3CD", default));
        Assert.False(await sut.VerifyAsync(challenge.Id, "AB3CD", default));
    }

    [Fact]
    public async Task Unknown_id_fails() =>
        Assert.False(await Create().VerifyAsync(Guid.NewGuid(), "AB3CD", default));
}