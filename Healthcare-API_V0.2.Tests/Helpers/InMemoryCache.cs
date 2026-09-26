using Healthcare_API_V0._2_Application.Interfaces;

namespace Healthcare_API_V0._2.Tests.Helpers;

/// <summary>In-memory ICacheService fake for service tests.</summary>
public sealed class InMemoryCache : ICacheService
{
    private readonly Dictionary<string, object> _store = new();
    public int Hits { get; private set; }
    public int Sets { get; private set; }
    public int Removes { get; private set; }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_store.TryGetValue(key, out var value) && value is T typed)
        {
            Hits++;
            return Task.FromResult<T?>(typed);
        }
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        Sets++;
        _store[key] = value!;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        Removes++;
        _store.Remove(key);
        return Task.CompletedTask;
    }
}
