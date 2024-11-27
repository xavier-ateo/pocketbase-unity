using UnityEngine;

public class SseExample : MonoBehaviour
{
    SseClient _sseClient;
    private WebGLSseClient _webGLSseClient;

    private void Start()
    {
        // _sseClient = new SseClient("http://localhost:3000/events");
        // _sseClient.OnMessage += OnMessage;
        // _sseClient.OnClose += OnClose;
        // _sseClient.OnError += Debug.LogException;

        _webGLSseClient = new("http://localhost:3000/events");
        _webGLSseClient.OnMessage += OnMessage;
        _webGLSseClient.OnClose += OnClose;
        _webGLSseClient.OnError += Debug.LogError;
        _webGLSseClient.Connect();
    }

    private void OnDestroy()
    {
        if (_sseClient is not null)
        {
            _sseClient.OnMessage -= OnMessage;
            _sseClient.OnClose -= OnClose;
            _sseClient.OnError -= Debug.LogException;

            // Don't forget to dispose the client on object destruction if it holds an SSE connection.
            _sseClient.Dispose();
        }

        if (_webGLSseClient is not null)
        {
            _webGLSseClient.OnMessage -= OnMessage;
            _webGLSseClient.OnClose -= OnClose;
            _webGLSseClient.OnError -= Debug.LogError;

            // Don't forget to dispose the client on object destruction if it holds an SSE connection.
            _webGLSseClient.Dispose();
        }
    }

    private static void OnClose()
    {
        Debug.Log("Connection closed.");
    }

    private static void OnMessage(SseMessage message)
    {
        Debug.Log($"Id: {message.Id}\nEvent: {message.Event}\nData: {message.Data}");
    }
}