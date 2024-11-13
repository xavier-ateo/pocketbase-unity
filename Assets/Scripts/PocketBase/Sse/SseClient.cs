using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using UnityEngine;
using Timer = System.Timers.Timer;

public class SseClient : IDisposable
{
    private static readonly List<int> DEFAULT_RETRY_TIMEOUTS = new() { 200, 300, 500, 1000, 1200, 1500, 2000 };
    private static readonly Regex LINE_REGEX = new(@"^(\w+)[\s\:]+(.*)?$", RegexOptions.Compiled);

    public event Action<SseMessage> OnMessage;
    public event Action OnClose;
    public event Action<Exception> OnError;

    private readonly HttpClient _httpClient;
    private readonly Uri _uri;
    private readonly int _maxRetry;
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    private Stream _stream;
    private int _retryAttempts;
    private Timer _retryTimer;

    public bool IsClosed { get; private set; }

    public SseClient(
        string url,
        int maxRetry = int.MaxValue,
        HttpClient httpClient = null)
    {
        _uri = new Uri(url);
        _maxRetry = maxRetry;

        _httpClient = httpClient ?? new HttpClient();
        Init();
    }

    private async void Init()
    {
        if (IsClosed)
        {
            return;
        }

        SseMessage sseMessage = new();

        try
        {
            // Use ResponseHeadersRead to start processing the stream immediately after receiving the headers. 
            // This is mandatory to supports the long-lived connection required for SSE.
            var response = await _httpClient.GetAsync(
                requestUri: _uri,
                completionOption: HttpCompletionOption.ResponseHeadersRead,
                cancellationToken: _cancellationTokenSource.Token
            );
            response.EnsureSuccessStatusCode();

            _retryAttempts = 0;

            _stream = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(_stream);

            while (true)
            {
                string line = await streamReader.ReadLineAsync();

                // Message end detected
                if (string.IsNullOrEmpty(line))
                {
                    OnMessage?.Invoke(sseMessage);
                    sseMessage = new(); // Reset for the next chunk
                    continue;
                }

                var match = LINE_REGEX.Match(line);

                if (!match.Success)
                    continue;

                string field = match.Groups[1].Value;
                string value = match.Groups[2].Value;

                switch (field)
                {
                    case "id":
                        sseMessage.Id = value;
                        break;

                    case "event":
                        sseMessage.Event = value;
                        break;

                    case "retry":
                        int.TryParse(value, out sseMessage.Retry);
                        break;

                    case "data":
                        sseMessage.Data = value;
                        break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Ignore
        }
        catch (Exception e)
        {
            Debug.LogException(e);

            // Most likely the client failed to establish a connection with the server
            OnError?.Invoke(e);
            Reconnect(sseMessage.Retry);
        }
    }

    private void Reconnect(int retryTimeout = 0)
    {
        if (_retryAttempts >= _maxRetry)
        {
            Close();
            return;
        }

        if (retryTimeout <= 0)
        {
            retryTimeout = _retryAttempts > DEFAULT_RETRY_TIMEOUTS.Count - 1
                ? DEFAULT_RETRY_TIMEOUTS.Last()
                : DEFAULT_RETRY_TIMEOUTS[_retryAttempts];
        }

        _retryTimer?.Dispose();

        _retryTimer = new Timer(retryTimeout);
        _retryTimer.Elapsed += (_, _) =>
        {
            _retryAttempts++;
            Init();
        };
    }

    private void Close()
    {
        if (IsClosed)
        {
            return;
        }

        IsClosed = true;

        OnMessage = null;
        OnError = null;

        OnClose?.Invoke();
    }

    public void Dispose()
    {
        Close();

        _cancellationTokenSource.Cancel();
        _retryTimer?.Dispose();
        _stream?.Dispose();
        _httpClient?.CancelPendingRequests();
        _httpClient?.Dispose();
    }
}