using UnityEngine;

public class SseExample : MonoBehaviour
{
    SseClient _sseClient;

    private void Start()
    {
        _sseClient = new SseClient("http://localhost:3000/events");
        _sseClient.OnMessage += OnMessage;
        _sseClient.OnClose += () => Debug.Log("Closed");
        _sseClient.OnError += Debug.LogException;
    }

    private void OnMessage(SseMessage message)
    {
        Debug.Log($"Id: {message.Id}\nEvent: {message.Event}\nData: {message.Data}");
    }

    private void OnDestroy()
    {
        _sseClient.Dispose();
    }
}