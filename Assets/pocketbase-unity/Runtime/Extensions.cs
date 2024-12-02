using System;
using System.Collections.Generic;
using System.Linq;

public static class DictionaryExtensions
{
    public static void RemoveWhere<TKey, TValue>(
        this IDictionary<TKey, TValue> dictionary,
        Func<KeyValuePair<TKey, TValue>, bool> predicate)
    {
        var keysToRemove = dictionary.Where(predicate).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove)
        {
            dictionary.Remove(key);
        }
    }
}