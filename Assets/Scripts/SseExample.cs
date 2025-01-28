using UnityEngine;

public class SseExample : MonoBehaviour
{
    private PocketBaseSdk.SseClient _sseClient;

    private void Start()
    {
        _sseClient = new("http://192.168.24.150:3000/events", maxRetry: 5);
        _sseClient.OnMessage += Debug.Log;
        _sseClient.OnError += Debug.LogException;
        _sseClient.OnClose += () => Debug.Log("Closed");
        _sseClient.Connect();
    }
}