using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace PocketBaseSdk
{
    public class AsyncAuthStore : AuthStore
    {
        private const string _tokenKey = "token";
        private const string _modelKey = "model";

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

        public override void Save(string newToken, RecordModel newModel)
        {
            base.Save(newToken, newModel);

            var encoded = JsonConvert.SerializeObject(new Dictionary<string, object>
            {
                ["token"] = newToken,
                ["record"] = newModel
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

            // Dictionary<string, object> decoded = new();
            RecordAuth decoded;

            try
            {
                decoded = JsonConvert.DeserializeObject<RecordAuth>(initial);
            }
            catch (Exception e)
            {
                Debug.Log(e.Message);
                return;
            }

            // return;
            //
            // try
            // {
            //     var raw = JsonConvert.DeserializeObject<Dictionary<string, object>>(initial);
            //     if (raw != null)
            //     {
            //         decoded = raw;
            //     }
            // }
            // catch
            // {
            //     return;
            // }
            //
            // string token = decoded.TryGetValue(_tokenKey, out object tokenValue) && tokenValue is string tokenString
            //     ? tokenString
            //     : string.Empty;
            //
            // var recordData = decoded.TryGetValue(_modelKey, out object modelValue) &&
            //                  modelValue is Dictionary<string, object> modelDict
            //     ? modelDict
            //     : new();
            //
            // var record = JsonConvert.DeserializeObject<RecordModel>(recordData.ToString() ?? "{}");

            Save(decoded.Token, decoded.Record);
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