using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.Networking.UnityWebRequest.Result;

public class WebGLSseClient : IDisposable
{
    private static readonly List<int> DEFAULT_RETRY_TIMEOUTS = new() { 200, 300, 500, 1000, 1200, 1500, 2000 };

    public event Action<SseMessage> OnMessage;
    public event Action<WebException> OnError;
    public event Action OnClose;

    private readonly string _url;
    private readonly int _maxRetry;
    private readonly Dictionary<string, string> _customHeaders;

    private bool _isClosed;
    private int _retryAttempts;
    private Coroutine _connectionCoroutine;

    // Track the last processed position to prevent duplicate processing
    private ulong _lastProcessedBytes;

    public WebGLSseClient(
        string url,
        int maxRetry = int.MaxValue,
        Dictionary<string, string> headers = null)
    {
        _url = url;
        _maxRetry = maxRetry;
        _customHeaders = headers ?? new Dictionary<string, string>();

        Application.quitting += Dispose;
    }

    public void Connect()
    {
        if (_isClosed)
            return;

        // Stop any existing connection attempt
        if (_connectionCoroutine != null)
        {
            CoroutineRunner.StopCoroutine(_connectionCoroutine);
        }

        _connectionCoroutine = CoroutineRunner.StartCoroutine(ConnectCoroutine());
    }
    
    public void Close() => Dispose();

    private IEnumerator ConnectCoroutine()
    {
        while (!_isClosed)
        {
            using var request = UnityWebRequest.Get(_url);

            foreach (var (key, value) in _customHeaders)
            {
                request.SetRequestHeader(key, value);
            }

            // Set SSE-specific headers
            request.SetRequestHeader("Accept", "text/event-stream");
            request.SetRequestHeader("Cache-Control", "no-cache");

            request.SendWebRequest();

            // Wait until headers are received
            while (!request.isDone)
            {
                // Check if headers are available
                if (request.responseCode != 0)
                {
                    break;
                }

                yield return null;
            }

            if (request.result is ConnectionError or ProtocolError)
            {
                OnError?.Invoke(new WebException($"Connection Error: {request.error}",
                    WebExceptionStatus.ConnectFailure));
                yield return new WaitForSeconds(GetRetryTimeoutSeconds());
                continue;
            }

            yield return ProcessStreamedResponseCoroutine(request);
        }
    }

    private IEnumerator ProcessStreamedResponseCoroutine(UnityWebRequest request)
    {
        while (!_isClosed && request.result is InProgress)
        {
            // Check if new data is available
            if (request.downloadedBytes > _lastProcessedBytes)
            {
                // Get only the new chunk of data
                string newData = request.downloadHandler.text.Substring((int)_lastProcessedBytes);

                ProcessChunk(newData);

                _lastProcessedBytes = request.downloadedBytes;
            }

            yield return null;
        }

        // Handle any final processing or error. Usually triggered on abruptly connection termination
        // (e.g. when the server goes down).
        if (request.result is not InProgress)
        {
            OnError?.Invoke(new WebException($"Stream ended: {request.error}", WebExceptionStatus.ConnectionClosed));
            Reconnect();
        }
    }

    private void ProcessChunk(string chunk)
    {
        if (string.IsNullOrEmpty(chunk))
            return;

        string[] lines = chunk.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        SseMessage sseMessage = new();

        foreach (string line in lines)
        {
            // Comment, ignore
            if (line.StartsWith(":"))
                continue;

            // Split each line into field and value at the first occurrence of ':'
            string[] parts = line.Split(':', 2);
            string field = parts.ElementAtOrDefault(0)?.Trim();
            string value = parts.ElementAtOrDefault(1)?.Trim();

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

        OnMessage?.Invoke(sseMessage);
    }

    private void Reconnect()
    {
        if (_isClosed)
            return;

        if (_retryAttempts < _maxRetry)
        {
            CoroutineRunner.StartCoroutine(ReconnectCoroutine());
        }
        else
        {
            Dispose();
        }
    }

    private IEnumerator ReconnectCoroutine()
    {
        yield return new WaitForSeconds(GetRetryTimeoutSeconds());
        Connect();
    }

    private float GetRetryTimeoutSeconds()
    {
        _retryAttempts++;

        return _retryAttempts > DEFAULT_RETRY_TIMEOUTS.Count - 1
            ? DEFAULT_RETRY_TIMEOUTS[^1] / 1000f
            : DEFAULT_RETRY_TIMEOUTS[_retryAttempts] / 1000f;
    }

    public void Dispose()
    {
        if (_isClosed)
            return;

        _isClosed = true;

        OnMessage = null;
        OnError = null;

        if (_connectionCoroutine != null)
        {
            CoroutineRunner.StopCoroutine(_connectionCoroutine);
        }

        OnClose?.Invoke();
        OnClose = null;

        Application.quitting -= Dispose;
    }
}