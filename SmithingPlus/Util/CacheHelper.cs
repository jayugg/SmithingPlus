using System;
using System.Collections.Generic;

namespace SmithingPlus.Util;

public static class CacheHelper
{
    public static TValue GetOrAdd<TKey, TValue>(
        IDictionary<TKey, TValue> cache,
        TKey key,
        Func<TValue> valueFactory,
        Action<TKey, TValue> onAdd = null)
    {
        if (cache.TryGetValue(key, out var value)) return value;
        value = valueFactory();
        if (value != null)
        {
            cache[key] = value;
            onAdd?.Invoke(key, value);
        }
        return value;
    }
}