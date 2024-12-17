using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace PocketBaseSdk
{
    public class AsyncAuthStore : AuthStore
    {
        private readonly Func<string, Task> _save;
        private readonly Func<Task> _clear;
        private readonly SyncQueue _queue = new();

        public AsyncAuthStore(
            Func<string, Task> save,
            string initial = null,
            Func<Task> clear = null)
        {
            _save = save;
            _clear = clear;

            LoadInitial(initial);
        }

        public override void Save(string newToken, RecordModel newRecord)
        {
            base.Save(newToken, newRecord);

            var encoded = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                ["token"] = newToken,
                ["record"] = newRecord
            });

            _queue.Enqueue(() => _save(encoded));
        }

        public override void Clear()
        {
            base.Clear();

            if (_clear is null)
            {
                _queue.Enqueue(() => _save(string.Empty));
            }
            else
            {
                _queue.Enqueue(() => _clear());
            }
        }

        private void LoadInitial(string initial)
        {
            if (string.IsNullOrEmpty(initial))
            {
                return;
            }

            try
            {
                var decoded = JsonConvert.DeserializeObject<RecordAuth>(initial);
                Save(decoded.Token, decoded.Record);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
            }
        }

        public static AsyncAuthStore PlayerPrefs => new(
            save: data =>
            {
                UnityEngine.PlayerPrefs.SetString("pb_auth", data);
                return Task.CompletedTask;
            },
            initial: UnityEngine.PlayerPrefs.GetString("pb_auth", string.Empty)
        );
    }
}