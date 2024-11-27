using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class WebGLSseClient : IDisposable
{
    private static readonly List<int> DEFAULT_RETRY_TIMEOUTS = new() { 200, 300, 500, 1000, 1200, 1500, 2000 };
    private static readonly Regex LINE_REGEX = new(@"^(\w+)[\s\:]+(.*)?$", RegexOptions.Compiled);

    public event Action<SseMessage> OnMessage;
    public event Action<string> OnError;
    public event Action OnClose;

    private readonly string _url;
    private readonly int _maxRetry;
    private readonly Dictionary<string, string> _customHeaders;

    private bool _isClosed;
    private int _retryAttempts;
    private Coroutine _connectionCoroutine;
    
    // Track the last processed position to prevent duplicate processing
    private long _lastProcessedBytes;

    public WebGLSseClient(
        string url, 
        int maxRetry = int.MaxValue, 
        Dictionary<string, string> customHeaders = null)
    {
        _url = url;
        _maxRetry = maxRetry;
        _customHeaders = customHeaders ?? new Dictionary<string, string>();

        Application.quitting += Dispose;
    }

    public void Connect()
    {
        if (_isClosed)
            return;

        // Stop any existing connection attempt
        if (_connectionCoroutine != null)
            CoroutineRunner.StopCoroutine(_connectionCoroutine);

        // Start a new connection coroutine
        _connectionCoroutine = CoroutineRunner.StartCoroutine(ConnectToServerSentEvents());
    }

    private IEnumerator ConnectToServerSentEvents()
    {
        while (!_isClosed)
        {
            using (UnityWebRequest www = UnityWebRequest.Get(_url))
            {
                // Set custom headers if any
                foreach (var header in _customHeaders)
                {
                    www.SetRequestHeader(header.Key, header.Value);
                }

                // Set SSE-specific headers
                www.SetRequestHeader("Accept", "text/event-stream");
                www.SetRequestHeader("Cache-Control", "no-cache");

                // Send request and wait for response
                www.SendWebRequest();

                // Wait until headers are received
                while (!www.isDone)
                {
                    // Check if headers are available
                    if (www.responseCode != 0)
                    {
                        break;
                    }
                    yield return null;
                }

                // Check for connection errors
                if (www.result == UnityWebRequest.Result.ConnectionError)
                {
                    OnError?.Invoke($"Connection Error: {www.error}");
                    yield return new WaitForSeconds(GetRetryTimeout());
                    continue;
                }

                // Check for protocol errors
                if (www.result == UnityWebRequest.Result.ProtocolError)
                {
                    OnError?.Invoke($"Protocol Error: {www.error}");
                    yield return new WaitForSeconds(GetRetryTimeout());
                    continue;
                }

                // If successful, start processing the stream
                yield return ProcessStreamedResponse(www);
            }
        }
    }

    private IEnumerator ProcessStreamedResponse(UnityWebRequest www)
    {
        SseMessage currentMessage = new SseMessage();
        StringBuilder dataBuffer = new StringBuilder();

        while (!_isClosed && www.result == UnityWebRequest.Result.InProgress)
        {
            // Check if new data is available
            if (www.downloadedBytes > (ulong)_lastProcessedBytes)
            {
                // Get only the new chunk of data
                string newData = www.downloadHandler.text.Substring((int)_lastProcessedBytes);
                _lastProcessedBytes = (long)www.downloadedBytes;

                // Process each line in the new data
                string[] lines = newData.Split('\n');
                foreach (string line in lines)
                {
                    ProcessLine(line, ref currentMessage, dataBuffer);
                }
            }

            yield return null;
        }

        // Handle any final processing or error
        if (www.result != UnityWebRequest.Result.InProgress)
        {
            OnError?.Invoke($"Stream ended: {www.result}");
            Reconnect();
        }
    }

    private void ProcessLine(string line, ref SseMessage message, StringBuilder dataBuffer)
    {
        // Skip empty lines
        if (string.IsNullOrWhiteSpace(line))
        {
            // Empty line signals end of event
            if (string.IsNullOrEmpty(message.Data))
            {
                // Trim any trailing newline from data
                message.Data = dataBuffer.ToString().TrimEnd('\n');
                OnMessage?.Invoke(message);
                
                // Reset for next event
                message = new SseMessage();
                dataBuffer.Clear();
            }
            return;
        }

        var match = LINE_REGEX.Match(line);
        if (!match.Success)
            return;

        string field = match.Groups[1].Value;
        string value = match.Groups[2]?.Value ?? "";

        // Parse different SSE fields
        switch (field)
        {
            case "id":
                message.Id = value;
                break;
            case "event":
                message.Event = value;
                break;
            case "retry":
                int.TryParse(value, out message.Retry);
                break;
            case "data":
                dataBuffer.AppendLine(value);
                break;
        }
    }

    private void Reconnect()
    {
        if (_isClosed)
            return;

        // Restart connection if not closed and max retries not exceeded
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
        yield return new WaitForSeconds(GetRetryTimeout());
        Connect();
    }

    private float GetRetryTimeout()
    {
        _retryAttempts++;

        return _retryAttempts > DEFAULT_RETRY_TIMEOUTS.Count - 1
            ? DEFAULT_RETRY_TIMEOUTS[DEFAULT_RETRY_TIMEOUTS.Count - 1] / 1000f
            : DEFAULT_RETRY_TIMEOUTS[_retryAttempts] / 1000f;
    }

    public void Dispose()
    {
        if (_isClosed)
            return;

        _isClosed = true;

        // Clear event subscriptions
        OnMessage = null;
        OnError = null;

        // Stop any running coroutine
        if (_connectionCoroutine != null)
            CoroutineRunner.StopCoroutine(_connectionCoroutine);

        // Invoke close event
        OnClose?.Invoke();
        OnClose = null;

        // Unsubscribe from application quitting
        Application.quitting -= Dispose;
    }
}