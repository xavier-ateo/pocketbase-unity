using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using UnityEngine.Networking;
using static UnityEngine.Networking.UnityWebRequest.Result;

namespace PocketBaseSdk
{
    public class SseClient : IDisposable
    {
        private static readonly List<int> _defaultRetryTimeouts = new() { 200, 300, 500, 1000, 1200, 1500, 2000 };

        public event Action<SseMessage> OnMessage;
        public event Action<WebException> OnError;
        public event Action OnClose;

        private readonly string _url;
        private readonly int _maxRetry;
        private readonly Dictionary<string, string> _customHeaders;

        private UnityWebRequest _request;
        private SseMessage _sseMessage;
        private int _retryAttempts;
        private Coroutine _connectionCoroutine;
        private bool _isClosed;
        private bool _receivedAnyMessage;

        public SseClient(
            string url,
            int maxRetry = int.MaxValue,
            Dictionary<string, string> headers = null)
        {
            _url = url;
            _maxRetry = maxRetry;
            _customHeaders = headers ?? new Dictionary<string, string>();

            Application.quitting += Close;
        }

        public void Connect()
        {
            if (_isClosed)
                return;

            // Stop any existing connection attempt
            if (_connectionCoroutine is not null)
            {
                CoroutineRunner.StopCoroutine(_connectionCoroutine);
            }

            CleanupRequest();
            _connectionCoroutine = CoroutineRunner.StartCoroutine(ConnectCoroutine());
        }

        public void Close()
        {
            if (_isClosed)
                return;

            _isClosed = true;
            _sseMessage = null;

            // Cancel any in-flight request before disposing so cleanup is synchronous.
            try { _request?.Abort(); } catch { /* may already be disposed */ }

            CleanupRequest();

            Application.quitting -= Close;

            OnMessage = null;
            OnError = null;

            if (_connectionCoroutine != null)
            {
                CoroutineRunner.StopCoroutine(_connectionCoroutine);
            }

            OnClose?.Invoke();
            OnClose = null;
        }

        public void Dispose() => Close();

        private IEnumerator ConnectCoroutine()
        {
            _sseMessage = new SseMessage();
            _receivedAnyMessage = false;

            // Local reference: a successor coroutine may replace _request before this
            // iterator's finally block runs.
            var localRequest = new UnityWebRequest(_url);
            _request = localRequest;
            bool disposedNormally = false;

            try
            {
                foreach (var (key, value) in _customHeaders)
                {
                    localRequest.SetRequestHeader(key, value);
                }

                // Set SSE-specific headers
                localRequest.SetRequestHeader("Accept", "text/event-stream");
                localRequest.SetRequestHeader("Cache-Control", "no-cache");

                // SSE streams are long-lived; disable the per-request timeout.
                localRequest.timeout = 0;

                localRequest.downloadHandler = new DownloadHandlerSseMessage(OnMessageReceived);
                localRequest.SendWebRequest();

                while (localRequest.result is InProgress)
                {
                    if (_isClosed)
                    {
                        yield break;
                    }
                    yield return null;
                }

                if (_isClosed)
                {
                    yield break;
                }

                var requestResult = localRequest.result;
                var requestError = localRequest.error;
                int retryTimeout = _sseMessage?.Retry ?? 0;

                if (_receivedAnyMessage)
                {
                    _retryAttempts = 0;
                }

                // Dispose now (before yielding to Reconnect) instead of waiting for the finalizer.
                if (_request == localRequest)
                {
                    _request = null;
                }
                
                localRequest.Dispose();
                disposedNormally = true;

                if (requestResult == Success)
                {
                    OnError?.Invoke(null);
                    yield return Reconnect(retryTimeout);
                    yield break;
                }

                WebException webEx = new(
                    $"{requestResult}: {requestError} ({_url})",
                    requestResult switch
                    {
                        ConnectionError => WebExceptionStatus.ConnectFailure,
                        ProtocolError => WebExceptionStatus.ProtocolError,
                        DataProcessingError => WebExceptionStatus.ReceiveFailure,
                        _ => WebExceptionStatus.UnknownError
                    }
                );

                OnError?.Invoke(webEx);

                yield return Reconnect(retryTimeout);
            }
            finally
            {
                // Release the request if normal flow didn't (e.g. StopCoroutine, scene unload).
                if (!disposedNormally)
                {
                    if (_request == localRequest)
                    {
                        _request = null;
                    }
                    try { localRequest.Abort(); } catch { /* may already be disposed */ }
                    try { localRequest.Dispose(); } catch { /* may already be disposed */ }
                }
            }
        }

        private void OnMessageReceived(SseMessage sseMessage)
        {
            _receivedAnyMessage = true;
            _sseMessage = sseMessage;
            OnMessage?.Invoke(sseMessage);
        }

        private IEnumerator Reconnect(int retryTimeout = 0)
        {
            if (_isClosed)
                yield break;

            if (_retryAttempts >= _maxRetry)
            {
                Close();
                yield break;
            }

            if (retryTimeout <= 0)
            {
                if (_retryAttempts > _defaultRetryTimeouts.Count - 1)
                    retryTimeout = _defaultRetryTimeouts.Last();
                else
                    retryTimeout = _defaultRetryTimeouts[_retryAttempts];
            }

            Debug.Log($"Retrying in {retryTimeout} ms");

            float retryTimeoutSeconds = retryTimeout / 1000f;
            yield return new WaitForSeconds(retryTimeoutSeconds);

            _retryAttempts++;
            Connect();
        }

        private void CleanupRequest()
        {
            _request?.Dispose();
            _request = null;
        }
    }
}
