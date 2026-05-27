namespace ChozaMaui.Services;

public sealed class SessionCacheService
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly Dictionary<string, CacheEntry> _entries = new();

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        TimeSpan ttl,
        Func<Task<T>> factory)
    {
        var now = DateTimeOffset.UtcNow;

        await _gate.WaitAsync();
        try
        {
            if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAt > now)
                return (T)entry.Value;
        }
        finally
        {
            _gate.Release();
        }

        var value = await factory();

        await _gate.WaitAsync();
        try
        {
            _entries[key] = new CacheEntry(value!, now.Add(ttl));
        }
        finally
        {
            _gate.Release();
        }

        return value;
    }

    public async Task RemoveAsync(string key)
    {
        await _gate.WaitAsync();
        try
        {
            _entries.Remove(key);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        await _gate.WaitAsync();
        try
        {
            var keys = _entries.Keys
                .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
                .ToList();

            foreach (var key in keys)
                _entries.Remove(key);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl)
    {
        await _gate.WaitAsync();
        try
        {
            _entries[key] = new CacheEntry(value!, DateTimeOffset.UtcNow.Add(ttl));
        }
        finally
        {
            _gate.Release();
        }
    }

    private sealed record CacheEntry(object Value, DateTimeOffset ExpiresAt);
}