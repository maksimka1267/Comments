namespace Comments.Domain.Abstractions;

public static class CacheScopes
{
    public const string CommentList = "comments";
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct);
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct);

    /// <summary>Текущая версия набора кэшированных данных (0, если ещё не менялась).</summary>
    Task<long> GetVersionAsync(string scope, CancellationToken ct);

    /// <summary>Увеличивает версию: все записи со старой версией становятся недействительными.</summary>
    Task BumpVersionAsync(string scope, CancellationToken ct);
}