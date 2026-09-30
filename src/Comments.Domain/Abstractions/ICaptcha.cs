namespace Comments.Domain.Abstractions;

public sealed record CaptchaChallenge(Guid Id, byte[] Image);

public interface ICaptchaService
{
    Task<CaptchaChallenge> CreateAsync(CancellationToken ct);
    Task<bool> VerifyAsync(Guid id, string? answer, CancellationToken ct);
}

public interface ICaptchaStore
{
    Task SetAsync(Guid id, string answer, TimeSpan ttl, CancellationToken ct);

    /// <summary>Возвращает ответ и сразу удаляет его (капча одноразовая).</summary>
    Task<string?> TakeAsync(Guid id, CancellationToken ct);
}