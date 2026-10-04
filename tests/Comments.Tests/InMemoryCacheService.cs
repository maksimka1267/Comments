using System.Text.Json;

using Comments.Domain.Abstractions;

namespace Comments.Tests;

public sealed class InMemoryCacheService : ICacheService
{
    private readonly Dictionary<string, string> _values = new();
    private readonly Dictionary<string, long> _versions = new();

    public Task<T?> GetAsync<T>(string key, CancellationToken ct) =>
        Task.FromResult(_values.TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json) : default);

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct)
    {
        _values[key] = JsonSerializer.Serialize(value);
        return Task.CompletedTask;
    }

    public Task<long> GetVersionAsync(string scope, CancellationToken ct) =>
        Task.FromResult(_versions.GetValueOrDefault(scope));

    public Task BumpVersionAsync(string scope, CancellationToken ct)
    {
        _versions[scope] = _versions.GetValueOrDefault(scope) + 1;
        return Task.CompletedTask;
    }
}