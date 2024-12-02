using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

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

        Dictionary<string, object> decoded;

        try
        {
            decoded = JsonConvert.DeserializeObject<Dictionary<string, object>>(initial);
            var token = decoded["token"] as string ?? string.Empty;
            var recordString = decoded["record"].ToString();
            var rawModel = JsonConvert.DeserializeObject<Dictionary<string, object>>(recordString);

            RecordModel model = null;

            if (rawModel.ContainsKey("collectionId") ||
                rawModel.ContainsKey("collectionName") ||
                rawModel.ContainsKey("verified") ||
                rawModel.ContainsKey("emailVisibility"))
            {
                model = JsonConvert.DeserializeObject<UserModel>(recordString);
            }
            else if (rawModel.ContainsKey("id"))
            {
                model = JsonConvert.DeserializeObject<AdminModel>(recordString);
            }

            Save(token, model);
        }
        catch (Exception)
        {
            Debug.LogWarning("Failed to load initial auth data.");
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