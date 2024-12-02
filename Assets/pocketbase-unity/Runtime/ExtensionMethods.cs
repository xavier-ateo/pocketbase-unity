using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace PocketBaseSdk
{
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
}