using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

public static class ExtensionMethods
{
    public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<object>();
        asyncOp.completed += _ => { tcs.SetResult(null); };
        return ((Task)tcs.Task).GetAwaiter();
    }

    public static bool TryAddNonNull<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TValue value)
    {
        if (value is null)
            return false;

        if (value is string str && (string.IsNullOrWhiteSpace(str) || string.IsNullOrEmpty(str)))
        {
            return false;
        }

        return dictionary.TryAdd(key, value);
    }
}