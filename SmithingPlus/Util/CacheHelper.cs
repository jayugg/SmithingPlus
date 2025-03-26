using System;
using System.Collections.Generic;

namespace SmithingPlus.Util;

public static class CacheHelper
{
    /// <summary>
    /// Get value from cache or add it if it doesn't exist.
    /// </summary>
    /// <param name="cache"> The cache to get or add the value from. </param>
    /// <param name="key"> The key to get or add the value with. </param>
    /// <param name="valueFactory"> The factory to create the value if it doesn't exist. </param>
    /// <param name="onAdd"> The action to perform when the value is added. </param>
    /// <typeparam name="TKey"> The type of the key. </typeparam>
    /// <typeparam name="TValue"> The type of the value. </typeparam>
    /// <returns></returns>
    public static TValue GetOrAdd<TKey, TValue>(
        IDictionary<TKey, TValue> cache,
        TKey key,
        Func<TValue> valueFactory,
        Action<TKey, TValue> onAdd = null)
    {
        if (cache.TryGetValue(key, out var value)) return value;
        value = valueFactory();
        if (value == null) return default;
        cache[key] = value;
        onAdd?.Invoke(key, value);
        return value;
    }
}