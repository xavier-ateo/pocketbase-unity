using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
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

        static TaskScheduler UnitySynchronizationContext => TaskScheduler.FromCurrentSynchronizationContext();

        // Input void, output void
        public static Task ContinueWithOnMainThread(this Task task, Action<Task> continuation) =>
            task.ContinueWith(continuation, UnitySynchronizationContext);

        // Input void, output result
        public static Task<TResult> ContinueWithOnMainThread<TResult>(
            this Task task,
            Func<Task, TResult> continuation) =>
            task.ContinueWith(continuation, UnitySynchronizationContext);

        // Input value, output result
        public static Task<TResult> ContinueWithOnMainThread<TInput, TResult>(
            this Task<TInput> task,
            Func<Task<TInput>, TResult> continuation) =>
            task.ContinueWith(continuation, UnitySynchronizationContext);
    }
}