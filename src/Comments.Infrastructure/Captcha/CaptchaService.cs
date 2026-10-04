using Comments.Domain.Abstractions;

namespace Comments.Infrastructure.Captcha;

public sealed class CaptchaService(
    ICaptchaCodeGenerator generator,
    CaptchaImageRenderer renderer,
    ICaptchaStore store) : ICaptchaService
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<CaptchaChallenge> CreateAsync(CancellationToken ct)
    {
        var code = generator.Generate();
        var id = Guid.NewGuid();

        await store.SetAsync(id, code, Ttl, ct);
        return new CaptchaChallenge(id, renderer.Render(code));
    }

    public async Task<bool> VerifyAsync(Guid id, string? answer, CancellationToken ct)
    {
        var expected = await store.TakeAsync(id, ct);

        return expected is not null
            && answer is not null
            && string.Equals(expected, answer.Trim(), StringComparison.OrdinalIgnoreCase);
    }
}