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
    public class SseClient
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

            _connectionCoroutine = CoroutineRunner.StartCoroutine(ConnectCoroutine());
        }

        public void Close()
        {
            if (_isClosed)
                return;

            _isClosed = true;
            _sseMessage = null;
            _request?.Dispose();
            _request = null;

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

        private IEnumerator ConnectCoroutine()
        {
            _sseMessage = new SseMessage();
            _request = new UnityWebRequest(_url);

            foreach (var (key, value) in _customHeaders)
            {
                _request.SetRequestHeader(key, value);
            }

            // Set SSE-specific headers
            _request.SetRequestHeader("Accept", "text/event-stream");
            _request.SetRequestHeader("Cache-Control", "no-cache");

            _request.downloadHandler = new DownloadHandlerSseMessage(OnMessageReceived);
            _request.SendWebRequest();

            while (_request.result is InProgress)
            {
                yield return null;
            }

            if (_request.result is ConnectionError or ProtocolError)
            {
                WebException webEx = new(
                    $"{_request.result}: {_request.error} ({_url})",
                    _request.result switch
                    {
                        ConnectionError => WebExceptionStatus.ConnectFailure,
                        ProtocolError => WebExceptionStatus.ProtocolError,
                        _ => WebExceptionStatus.UnknownError
                    }
                );

                OnError?.Invoke(webEx);

                yield return Reconnect(_sseMessage.Retry);
            }
        }

        private void OnMessageReceived(SseMessage sseMessage)
        {
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

            Debug.Log($"Retrying in {retryTimeout} ms");

            if (retryTimeout <= 0)
            {
                if (_retryAttempts > _defaultRetryTimeouts.Count - 1)
                    retryTimeout = _defaultRetryTimeouts.Last();
                else
                    retryTimeout = _defaultRetryTimeouts[_retryAttempts];
            }

            float retryTimeoutSeconds = retryTimeout / 1000f;
            yield return new WaitForSeconds(retryTimeoutSeconds);

            _retryAttempts++;
            Connect();
        }
    }
}