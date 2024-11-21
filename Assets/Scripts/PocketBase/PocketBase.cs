using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using UnityEngine.Networking;
using static UnityEngine.Networking.UnityWebRequest.Result;

public class PocketBase
{
    public HttpClient HttpClient { get; }
    public AuthStore AuthStore { get; }
    public AdminService Admins { get; }
    public CollectionService Collections { get; }
    public FileService Files { get; }
    public RealtimeService Realtime { get; }

    private readonly string _baseUrl;
    private readonly string _lang;
    private readonly Dictionary<string, RecordService> _recordServices = new();

    public PocketBase(
        string baseUrl,
        string lang = "en-US",
        AuthStore authStore = null,
        HttpClient httpClient = null)
    {
        AuthStore = authStore ?? new();
        HttpClient = httpClient ?? new();

        Admins = new AdminService(this);
        Collections = new CollectionService(this);
        Files = new FileService(this);
        Realtime = new RealtimeService(this);

        _baseUrl = baseUrl;
        _lang = lang;
    }

    public RecordService Collection(string collectionIdOrName)
    {
        if (_recordServices.TryGetValue(collectionIdOrName, out RecordService service))
        {
            return service;
        }

        service = new RecordService(this, collectionIdOrName);
        _recordServices.Add(collectionIdOrName, service);

        return service;
    }

    public async Task<T> Send<T>(
        string path,
        string method = "GET",
        Dictionary<string, string> headers = null,
        Dictionary<string, object> query = null,
        object body = null,
        List<IMultipartFormSection> files = null)
    {
        Uri url = BuildUrl(path, query);

        headers ??= new();
        headers.TryAdd("Accept-Language", _lang);

        if (AuthStore.IsValid())
        {
            headers.TryAdd("Authorization", AuthStore.Token);
        }

        using var req = files?.Count > 0
            ? MultipartRequest(method, url, headers, body, files)
            : JsonRequest(method, url, headers, body);

        req.downloadHandler = new DownloadHandlerBuffer();

        await req.SendWebRequest();

        if (req.result is not Success)
        {
            throw new ClientException
            (
                url: url,
                statusCode: (int)req.responseCode,
                originalError: req.downloadHandler.text,
                response: JsonConvert.DeserializeObject<Dictionary<string, object>>(req.downloadHandler.text)
            );
        }

        try
        {
            var record = JsonConvert.DeserializeObject<T>(req.downloadHandler.text);
            return record;
        }
        catch (JsonSerializationException)
        {
            return default;
        }
    }

    public Uri BuildUrl(string path, Dictionary<string, object> queryParameters = null)
    {
        string url = _baseUrl + (_baseUrl.EndsWith("/") ? "" : "/");

        if (!string.IsNullOrEmpty(path))
        {
            url += path.StartsWith("/") ? path.Substring(1) : path;
        }

        string query = NormalizeQueryParameters(queryParameters);

        var uriBuilder = new UriBuilder(url)
        {
            Query = string.IsNullOrEmpty(query) ? string.Empty : query
        };

        return uriBuilder.Uri;
    }

    private string NormalizeQueryParameters(Dictionary<string, object> queryParameters)
    {
        if (queryParameters == null || queryParameters.Count == 0)
        {
            return string.Empty;
        }

        var query = HttpUtility.ParseQueryString(string.Empty);

        foreach (var param in queryParameters)
        {
            query[param.Key] = param.Value.ToString();
        }

        return query.ToString();
    }

    private UnityWebRequest JsonRequest(
        string method,
        Uri url,
        Dictionary<string, string> headers = null,
        object body = null)
    {
        UnityWebRequest request = new UnityWebRequest(url, method);

        if (body is not null)
        {
            string jsonBody = JsonConvert.SerializeObject(body);
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.uploadHandler.contentType = "application/json";
        }

        if (headers is { Count: > 0 })
        {
            foreach (var (key, value) in headers)
            {
                request.SetRequestHeader(key, value);
            }
        }

        if (headers == null || !headers.ContainsKey("Content-Type"))
        {
            request.SetRequestHeader("Content-Type", "application/json");
        }

        request.downloadHandler = new DownloadHandlerBuffer();

        return request;
    }

    private UnityWebRequest MultipartRequest(
        string method,
        Uri url,
        Dictionary<string, string> headers,
        object body,
        List<IMultipartFormSection> files)
    {
        List<IMultipartFormSection> formData = new();

        if (body is not null)
        {
            string json = JsonConvert.SerializeObject(body);
            formData.Add(new MultipartFormDataSection("@jsonPayload", json));
        }

        if (files?.Count > 0)
        {
            formData.AddRange(files);
        }

        var request = UnityWebRequest.Post(url, formData);
        // overriding the method to be able to use PATCH
        request.method = method;
        
        if (headers is { Count: > 0 })
        {
            foreach (var (key, value) in headers)
            {
                request.SetRequestHeader(key, value);
            }
        }

        return request;
    }
}

public readonly struct Void
{
}