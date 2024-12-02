using System.Collections;
using UnityEngine;

/// <summary>
/// A static utility class to run coroutines from any context without requiring a MonoBehaviour
/// </summary>
public static class CoroutineRunner
{
    private class CoroutineHost : MonoBehaviour
    {
    }

    private static CoroutineHost _host;

    /// <summary>
    /// Initializes the coroutine runner if not already set up
    /// </summary>
    private static void EnsureCoroutineHost()
    {
        if (_host == null)
        {
            GameObject hostObject = new("[CoroutineRunner]");
            _host = hostObject.AddComponent<CoroutineHost>();
            Object.DontDestroyOnLoad(hostObject);
        }
    }

    /// <summary>
    /// Starts a coroutine and returns a unique identifier
    /// </summary>
    public static Coroutine StartCoroutine(IEnumerator routine)
    {
        EnsureCoroutineHost();
        return _host.StartCoroutine(routine);
    }

    /// <summary>
    /// Stops a previously started coroutine
    /// </summary>
    public static void StopCoroutine(Coroutine coroutine)
    {
        if (_host != null && coroutine != null)
        {
            _host.StopCoroutine(coroutine);
        }
    }
}