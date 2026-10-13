using System.Collections.Generic;

namespace BetterUI.Utils;

public static class DictionaryExtensions
{
    // Mimics the modern .NET Core TryAdd behavior for legacy .NET Framework
    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
    {
        if (dictionary == null) throw new System.ArgumentNullException(nameof(dictionary));
        
        if (!dictionary.ContainsKey(key))
        {
            dictionary.Add(key, value);
            return true;
        }
        return false;
    }
}