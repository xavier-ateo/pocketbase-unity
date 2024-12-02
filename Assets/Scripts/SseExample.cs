using TMPro;
using UnityEngine;

public class SseExample : MonoBehaviour
{
    [SerializeField] private TMP_Text _text;
    
    private SseClient _webGLSseClient;

    private void Start()
    {
        _webGLSseClient = new("http://192.168.24.150:3000/events", maxRetry: 5);
        _webGLSseClient.OnMessage += OnMessage;
        _webGLSseClient.OnClose += OnClose;
        _webGLSseClient.OnError += Debug.LogError;
        _webGLSseClient.Connect();
    }

    private void OnDestroy()
    {
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

    private void OnMessage(SseMessage message)
    {
        Debug.Log($"Id: {message.Id}\nEvent: {message.Event}\nData: {message.Data}");
        _text.text += $"Id: {message.Id}\nEvent: {message.Event}\nData: {message.Data}\n\n";
    }
}