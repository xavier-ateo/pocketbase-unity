using UnityEngine;

public class SseExample : MonoBehaviour
{
    SseClient _sseClient;

    private void Start()
    {
        _sseClient = new SseClient("http://localhost:3000/events");
        _sseClient.OnMessage += OnMessage;
        _sseClient.OnClose += OnClose;
        _sseClient.OnError += Debug.LogException;
    }

    private void OnDestroy()
    {
        if (_sseClient is null)
            return;

        _sseClient.OnMessage -= OnMessage;
        _sseClient.OnClose -= OnClose;
        _sseClient.OnError -= Debug.LogException;

        // Don't forget to dispose the client on object destruction if it holds an SSE connection.
        _sseClient.Dispose();
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