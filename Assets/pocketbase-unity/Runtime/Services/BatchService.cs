using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using UnityEngine.Networking;

namespace PocketBaseSdk
{
    /// <summary>
    /// The service that handles the Batch/transactional APIs.
    ///
    /// Usually shouldn't be initialized manually and instead
    /// <see cref="PocketBase.CreateBatch"/> should be used.
    /// </summary>
    public class BatchService : BaseService
    {
        private readonly List<BatchRequest> _requests = new();
        private readonly Dictionary<string, SubBatchService> _subs = new();
        private readonly PocketBase _dummyClient = new("/");

        public BatchService(PocketBase client) : base(client)
        {
        }

        /// <summary>
        /// Starts constructing a batch request entry for the specified collection.
        /// </summary>
        public SubBatchService Collection(string collectionIdOrName)
        {
            if (_subs.TryGetValue(collectionIdOrName, out var subService))
            {
                return subService;
            }

            subService = new SubBatchService(this, collectionIdOrName);
            _subs[collectionIdOrName] = subService;

            return subService;
        }

        /// <summary>
        /// Sends the batch requests.
        /// </summary>
        public Task<List<BatchResult>> Send(
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            List<IMultipartFormSection> files = new();
            List<Dictionary<string, object>> jsonBody = new();

            for (var i = 0; i < _requests.Count; i++)
            {
                BatchRequest req = _requests[i];

                jsonBody.Add(new Dictionary<string, object>
                {
                    ["method"] = req.Method,
                    ["url"] = req.Url,
                    ["headers"] = req.Headers,
                    ["body"] = req.Body
                });

                foreach (IMultipartFormSection reqFile in req.Files)
                {
                    files.Add(new MultipartFormFileSection(
                        name: $"requests.{i}.{reqFile.sectionName}",
                        data: reqFile.sectionData,
                        fileName: reqFile.fileName,
                        contentType: reqFile.contentType
                    ));
                }
            }

            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("requests", jsonBody);

            return _client.Send(
                "/api/batch",
                method: "POST",
                files: files,
                headers: headers,
                query: query,
                body: enrichedBody
            ).ContinueWith(t => t.Result.ToObject<List<BatchResult>>());
        }


        public class SubBatchService
        {
            private readonly BatchService _batch;
            private readonly string _collectionIdOrName;

            public SubBatchService(BatchService batch, string collectionIdOrName)
            {
                _batch = batch;
                _collectionIdOrName = collectionIdOrName;
            }

            /// <summary>
            /// Registers a record upsert request into the current batch queue.
            ///
            /// The request will be executed as update if 'bodyParams' have a
            /// valid existing record 'id' value, otherwise - create.
            /// </summary>
            public void Upsert(
                Dictionary<string, object> body = null,
                Dictionary<string, object> query = null,
                List<IMultipartFormSection> files = null,
                Dictionary<string, string> headers = null,
                string expand = null,
                string fields = null)
            {
                Dictionary<string, object> enrichedQuery = new(query ?? new());
                enrichedQuery.TryAddNonNull("expand", expand);
                enrichedQuery.TryAddNonNull("fields", fields);

                var request = new BatchRequest(
                    method: "PUT",
                    files: files,
                    url: _batch._dummyClient
                        .BuildUrl(
                            $"/api/collections/{HttpUtility.UrlEncode(_collectionIdOrName)}/records",
                            enrichedQuery)
                        .ToString(),
                    headers: headers,
                    body: body
                );

                _batch._requests.Add(request);
            }

            /// <summary>
            /// Registers a record create request into the current batch queue.
            /// </summary>
            public void Create(
                Dictionary<string, object> body = null,
                Dictionary<string, object> query = null,
                List<IMultipartFormSection> files = null,
                Dictionary<string, string> headers = null,
                string expand = null,
                string fields = null)
            {
                Dictionary<string, object> enrichedQuery = new(query ?? new());
                enrichedQuery.TryAddNonNull("expand", expand);
                enrichedQuery.TryAddNonNull("fields", fields);

                var request = new BatchRequest(
                    method: "POST",
                    files: files,
                    url: _batch._dummyClient
                        .BuildUrl(
                            $"/api/collections/{HttpUtility.UrlEncode(_collectionIdOrName)}/records",
                            enrichedQuery)
                        .ToString(),
                    headers: headers,
                    body: body
                );

                _batch._requests.Add(request);
            }

            /// <summary>
            /// Registers a record update request into the current batch queue.
            /// </summary>
            public void Update(
                string recordId,
                Dictionary<string, object> body = null,
                Dictionary<string, object> query = null,
                List<IMultipartFormSection> files = null,
                Dictionary<string, string> headers = null,
                string expand = null,
                string fields = null)
            {
                Dictionary<string, object> enrichedQuery = new(query ?? new());
                enrichedQuery.TryAddNonNull("expand", expand);
                enrichedQuery.TryAddNonNull("fields", fields);

                var request = new BatchRequest(
                    method: "PATCH",
                    files: files,
                    url: _batch._dummyClient
                        .BuildUrl(
                            $"/api/collections/{HttpUtility.UrlEncode(_collectionIdOrName)}/records/{HttpUtility.UrlEncode(recordId)}",
                            enrichedQuery)
                        .ToString(),
                    headers: headers,
                    body: body
                );

                _batch._requests.Add(request);
            }

            /// <summary>
            /// Registers a record delete request into the current batch queue.
            /// </summary>
            public void Delete(
                string recordId,
                Dictionary<string, object> body = null,
                Dictionary<string, object> query = null,
                List<IMultipartFormSection> files = null,
                Dictionary<string, string> headers = null)
            {
                var request = new BatchRequest(
                    method: "DELETE",
                    files: files,
                    url: _batch._dummyClient
                        .BuildUrl(
                            $"/api/collections/{HttpUtility.UrlEncode(_collectionIdOrName)}/records/{HttpUtility.UrlEncode(recordId)}",
                            query)
                        .ToString(),
                    headers: headers,
                    body: body
                );

                _batch._requests.Add(request);
            }
        }

        private class BatchRequest
        {
            public string Method { get; set; }
            public string Url { get; set; }
            public Dictionary<string, string> Headers { get; set; }
            public Dictionary<string, object> Body { get; set; }
            public List<IMultipartFormSection> Files { get; set; }

            public BatchRequest(
                string method,
                string url,
                Dictionary<string, string> headers,
                Dictionary<string, object> body,
                List<IMultipartFormSection> files)
            {
                Method = method;
                Url = url;
                Headers = headers ?? new();
                Body = body ?? new();
                Files = files ?? new();
            }
        }
    }
}