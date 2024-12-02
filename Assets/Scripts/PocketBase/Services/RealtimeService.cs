using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

public delegate void SubscriptionFunc(SseMessage e);

public delegate Task UnsubscribeFunc();

public class RealtimeService : BaseService
{
    private SseClient _sse;
    private readonly Dictionary<string, SubscriptionFunc> _subscriptions = new();

    public string ClientId { get; private set; }

    public RealtimeService(PocketBase client) : base(client)
    {
    }

    /// <summary>
    /// Register the subscription listener.
    /// </summary>
    /// <remarks>
    /// You can subscribe multiple times to the same topic.
    ///
    /// If the SSE connection is not started yet,
    /// this method will also initialize it.
    /// </remarks>
    /// <example>
    /// Here is an example listening to the connect/reconnect events:
    /// <code lang="csharp">
    /// pb.Realtime.Subscribe("PB_CONNECT", (e) {
    ///   Debug.Log("Connected: ${e}");
    /// });
    /// </code>
    /// </example>
    public async Task<UnsubscribeFunc> Subscribe(
        string topic,
        SubscriptionFunc listener,
        string expand = null,
        string filter = null,
        string fields = null,
        Dictionary<string, object> query = null,
        Dictionary<string, string> headers = null)
    {
        var key = topic;

        Dictionary<string, object> enrichedQuery = new(query ?? new());
        enrichedQuery.TryAddNonNull("expand", expand);
        enrichedQuery.TryAddNonNull("filter", filter);
        enrichedQuery.TryAddNonNull("fields", fields);

        // Serialize and append the topic options (if any)
        Dictionary<string, object> options = new();
        options.TryAddNonNull("query", enrichedQuery);
        options.TryAddNonNull("headers", headers);

        if (options.Count > 0)
        {
            var encoded = $"options={HttpUtility.UrlEncode(JsonConvert.SerializeObject(options))}";
            key += (key.Contains("?") ? "&" : "?") + encoded;
        }

        if (!_subscriptions.TryAdd(key, listener))
        {
            _subscriptions[key] += listener;
        }

        if (_sse == null)
        {
            await Connect();
        }
        else if (!string.IsNullOrEmpty(ClientId) && _subscriptions[key] != null)
        {
            await SubmitSubscriptions();
        }

        return async () => { await UnsubscribeByTopicAndListener(topic, listener); };
    }

    /// Unsubscribe from all subscription listeners with the specified topic.
    ///
    /// If <paramref name="topic"/> is not set, then this method will unsubscribe
    /// from all active subscriptions.
    ///
    /// This method is no-op if there are no active subscriptions.
    ///
    /// The related sse connection will be autoclosed if after the
    /// unsubscribe operation there are no active subscriptions left.
    public Task Unsubscribe(string topic = null)
    {
        var needToSubmit = false;

        if (string.IsNullOrEmpty(topic))
        {
            _subscriptions.Clear();
        }
        else
        {
            var subs = GetSubscriptionsByTopic(topic);

            foreach (string key in subs.Keys)
            {
                _subscriptions.Remove(key);
                needToSubmit = true;
            }
        }

        if (!HasNonEmptyTopic())
        {
            Disconnect();
            return Task.CompletedTask;
        }

        if (!string.IsNullOrEmpty(ClientId) && needToSubmit)
        {
            return SubmitSubscriptions();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Unsubscribe from all subscription listeners starting with
    /// the specified topic prefix.
    /// </summary>
    /// <remarks>
    /// This method is no-op if there are no active subscriptions
    /// with the specified topic prefix.
    ///
    /// The related sse connection will be autoclosed if after the
    /// unsubscribe operation there are no active subscriptions left.
    /// </remarks>
    public Task UnsubscribeByPrefix(string topicPrefix)
    {
        var beforeLength = _subscriptions.Count;

        _subscriptions.RemoveWhere(kvp => $"{kvp.Key}?".StartsWith(topicPrefix));

        if (beforeLength == _subscriptions.Count)
        {
            return Task.CompletedTask;
        }

        if (!HasNonEmptyTopic())
        {
            Disconnect();
            return Task.CompletedTask;
        }

        if (!string.IsNullOrEmpty(ClientId))
        {
            return SubmitSubscriptions();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Unsubscribe from all subscriptions matching the specified topic
    /// and listener function.
    /// </summary>
    /// <remarks>
    /// This method is no-op if there are no active subscription with
    /// the specified topic and listener.
    ///
    /// The related sse connection will be autoclosed if after the
    /// unsubscribe operation there are no active subscriptions left.
    /// </remarks>
    private Task UnsubscribeByTopicAndListener(
        string topic,
        SubscriptionFunc listener)
    {
        bool needToSubmit = false;

        var subs = GetSubscriptionsByTopic(topic);

        foreach (string key in subs.Keys)
        {
            if (_subscriptions[key] == null)
            {
                // Nothing to unsubscribe from.
                continue;
            }

            var beforeLength = _subscriptions[key]?.GetInvocationList().Length ?? 0;

            _subscriptions[key] -= listener;

            var afterLength = _subscriptions[key]?.GetInvocationList().Length ?? 0;

            if (beforeLength == afterLength)
            {
                // No changes, no need to submit.
                continue;
            }

            if (!needToSubmit && afterLength == 0)
            {
                needToSubmit = true;
            }
        }

        if (!HasNonEmptyTopic())
        {
            Disconnect();
            return Task.CompletedTask;
        }

        if (!string.IsNullOrEmpty(ClientId) && needToSubmit)
        {
            return SubmitSubscriptions();
        }

        return Task.CompletedTask;
    }

    private bool HasNonEmptyTopic()
    {
        return _subscriptions.Any(kvp => kvp.Value != null);
    }

    private Task Connect()
    {
        Disconnect();

        var completer = new TaskCompletionSource<bool>();
        string url = _client.BuildUrl("/api/realtime").AbsoluteUri;

        _sse = new SseClient(url);
        _sse.OnClose += () =>
        {
            Disconnect();

            if (!completer.Task.IsCompleted)
            {
                completer.SetException(new Exception("failed to establish SSE connection"));
            }
        };

        // Bind subscriptions listener
        _sse.OnMessage += msg =>
        {
            if (!_subscriptions.TryGetValue(msg.Event, out var subscription))
            {
                return;
            }

            subscription?.Invoke(msg);
        };

        // Resubmit local subscriptions on first reconnect
        _sse.OnMessage += async msg =>
        {
            if (msg.Event != "PB_CONNECT")
            {
                return;
            }

            ClientId = msg.Id;
            await SubmitSubscriptions();

            if (!completer.Task.IsCompleted)
            {
                completer.SetResult(true);
            }
        };

        _sse.OnError += e =>
        {
            Disconnect();
            completer.SetException(new Exception("failed to establish SSE connection", e));
        };
        
        _sse.Connect();

        return completer.Task;
    }

    private void Disconnect()
    {
        _sse?.Close();
        _sse = null;
        ClientId = string.Empty;
    }

    private Task SubmitSubscriptions()
    {
        return _client.Send<Void>(
            path: "/api/realtime",
            method: "POST",
            body: new
            {
                clientId = ClientId,
                subscriptions = _subscriptions.Keys.ToList()
            }
        );
    }

    private Dictionary<string, SubscriptionFunc> GetSubscriptionsByTopic(string topic)
    {
        topic = topic.Contains("?") ? topic : $"{topic}?";

        return _subscriptions.Where(kvp => kvp.Key.StartsWith(topic)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }
}