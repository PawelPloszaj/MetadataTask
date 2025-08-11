using System.Collections.Concurrent;

namespace FivetranClient.Infrastructure;

public class TtlDictionary<TKey, TValue> where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, (TValue Value, DateTime ExpiryDateTime)> _dictionary = new();

    public async Task<TValue> GetOrAddAsync(TKey key, Func<Task<TValue>> valueFactory, TimeSpan ttl)
    {
        if (_dictionary.TryGetValue(key, out var entry) && DateTime.UtcNow < entry.ExpiryDateTime)
        {
            return entry.Value;
        }

        var value = await valueFactory();
        _dictionary[key] = (value, DateTime.UtcNow.Add(ttl));
        return value;
    }

    public TValue GetOrAdd(TKey key, Func<TValue> valueFactory, TimeSpan ttl)
    {
        if (_dictionary.TryGetValue(key, out var entry) && DateTime.UtcNow < entry.ExpiryDateTime)
        {
            return entry.Value;
        }

        var value = valueFactory();
        _dictionary[key] = (value, DateTime.UtcNow.Add(ttl));
        return value;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (_dictionary.TryGetValue(key, out var entry) && DateTime.UtcNow < entry.ExpiryDateTime)
        {
            value = entry.Value;
            return true;
        }

        value = default!;
        return false;
    }
}