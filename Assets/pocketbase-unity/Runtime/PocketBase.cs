using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using static UnityEngine.Networking.UnityWebRequest.Result;

namespace PocketBaseSdk
{
    public class PocketBase
    {
        public AuthStore AuthStore { get; }
        public BackupService Backups { get; }
        public CollectionService Collections { get; }
        public CronService Crons { get; }
        public FileService Files { get; }
        public HealthService Health { get; }
        public LogService Logs { get; }
        public RealtimeService Realtime { get; }
        public SettingsService Settings { get; }

        private readonly string _baseUrl;
        private readonly string _lang;
        private readonly Dictionary<string, RecordService> _recordServices = new();

        public string BaseUrl => _baseUrl;
        public string Lang => _lang;

        public PocketBase(
            string baseUrl,
            string lang = "en-US",
            AuthStore authStore = null)
        {
            AuthStore = authStore ?? new();
            
            Backups = new BackupService(this);
            Collections = new CollectionService(this);
            Crons = new CronService(this);
            Files = new FileService(this);
            Health = new HealthService(this);
            Logs = new LogService(this);
            Realtime = new RealtimeService(this);
            Settings = new SettingsService(this);

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

        public async Task<JObject> Send(
            string path,
            string method = "GET",
            Dictionary<string, string> headers = null,
            Dictionary<string, object> query = null,
            object body = null,
            List<IMultipartFormSection> files = null)
        {
            var url = BuildUrl(path, query);

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
                var respone = new Dictionary<string, object>();
                
                try
                {
                    respone = JsonConvert.DeserializeObject<Dictionary<string, object>>(req.downloadHandler.text);
                }
                catch (Exception)
                {
                    if(!string.IsNullOrEmpty(req.downloadHandler.text))
                    {
                        respone.Add("error", req.downloadHandler.text)
                    }
                }

                throw new ClientException
                (
                    url: url,
                    statusCode: (int)req.responseCode,
                    originalError: req.downloadHandler.text,
                    response: respone
                );
            }

            try
            {
                var record = JObject.Parse(req.downloadHandler.text);
                return record;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Constructs a filter expression with placeholders populated from a map.<br/>
        /// Placeholder parameters are defined with the <c>{:paramName}</c> notation.<br/>
        /// </summary>
        /// <remarks>
        /// The following parameter values are supported:
        /// <list type="bullet">
        ///     <item>String (single quotes are auto escaped)</item>
        ///     <item>Number</item>
        ///     <item>Boolean</item>
        ///     <item>DateTime</item>
        ///     <item>null</item>
        /// </list>
        /// Everything else is converted to a string using JsonConvert.SerializeObject
        /// </remarks>
        public static string Filter(string expr, Dictionary<string, object> query = null)
        {
            if (query is not { Count: > 0 })
            {
                return expr;
            }

            foreach (var (key, value) in query)
            {
                object finalValue = value switch
                {
                    null or int or long or double or float or bool => value?.ToString(),
                    DateTime dateTime => $"'{dateTime.ToUniversalTime():yyyy-MM-dd HH:mm:ss.fff}Z'",
                    string str => $"'{str.Replace("'", "\\'")}'",
                    _ => $"'{JsonConvert.SerializeObject(value).Replace("'", "\\'")}'"
                };

                expr = expr.Replace($"{{:{key}}}", finalValue?.ToString());
            }

            return expr;
        }

        public string BuildUrl(string path, Dictionary<string, object> queryParameters = null)
        {
            string url = _baseUrl + (_baseUrl.EndsWith("/") ? "" : "/");

            if (!string.IsNullOrEmpty(path))
            {
                url += path.StartsWith("/") ? path.Substring(1) : path;
            }

            string query = NormalizeQueryParameters(queryParameters);

            if (!string.IsNullOrEmpty(query))
            {
                url += "?" + query;
            }

            return url;
        }

        /// <summary>
        /// Creates a new batch handler for sending multiple transactional
        /// create/update/upsert/delete collection requests in one network call.
        /// </summary>
        /// <example>
        /// <code>
        /// var batch = pb.CreateBatch();
        ///
        /// batch.Collection("example1").Create(body: ...);
        /// batch.Collection("example1").Update("RECORD_ID", body: ...);
        /// batch.Collection("example1").Delete("RECORD_ID", body: ...);
        /// batch.Collection("example1").Upsert(body: ...);
        ///
        /// await batch.Send();
        /// </code>
        /// </example>
        public BatchService CreateBatch()
        {
            return new BatchService(this);
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
            string url,
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
            string url,
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
}