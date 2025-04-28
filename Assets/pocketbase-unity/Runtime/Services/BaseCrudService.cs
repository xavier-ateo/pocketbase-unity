using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using UnityEngine.Networking;

namespace PocketBaseSdk
{
    /// <summary>
    /// Base generic crud service that is intended to be used by all other crud services.
    /// </summary>
    /// <typeparam name="T">The type of entity being managed by the CRUD service.</typeparam>
    public abstract class BaseCrudService<T> : BaseService
    {
        protected abstract string BaseCrudPath { get; }

        protected BaseCrudService(PocketBase client) : base(client)
        {
        }

        /// <summary>
        /// Returns a list with all item batch fetched at once.
        /// </summary>
        public Task<List<T>> GetFullList(
            int batch = 500,
            string expand = null,
            string filter = null,
            string sort = null,
            string fields = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            List<T> result = new();

            async Task<List<T>> Request(int page)
            {
                var list = await GetList(
                    skipTotal: true,
                    page: page,
                    perPage: batch,
                    expand: expand,
                    filter: filter,
                    sort: sort,
                    fields: fields,
                    query: query,
                    headers: headers
                );

                result.AddRange(list.Items);

                if (list.Items.Count == list.PerPage)
                {
                    await Request(page + 1);
                }

                return result;
            }

            return Request(1);
        }

        public async Task<ResultList<T>> GetList(
            int page = 1,
            int perPage = 30,
            bool skipTotal = false,
            string expand = null,
            string filter = null,
            string sort = null,
            string fields = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAdd("page", page);
            enrichedQuery.TryAdd("perPage", perPage);
            enrichedQuery.TryAdd("skipTotal", skipTotal);
            enrichedQuery.TryAddNonNull("expand", expand);
            enrichedQuery.TryAddNonNull("filter", filter);
            enrichedQuery.TryAddNonNull("sort", sort);
            enrichedQuery.TryAddNonNull("fields", fields);

            var result = await _client.Send(
                BaseCrudPath,
                query: enrichedQuery,
                headers: headers
            );

            return result.ToObject<ResultList<T>>();
        }

        public async Task<T> GetOne(
            string id,
            string expand = null,
            string fields = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ClientException
                (
                    url: _client.BuildUrl($"{BaseCrudPath}/"),
                    statusCode: 404,
                    response: new Dictionary<string, object>
                    {
                        ["code"] = 404,
                        ["message"] = "Missing required record id.",
                        ["data"] = new()
                    }
                );
            }

            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAddNonNull("expand", expand);
            enrichedQuery.TryAddNonNull("fields", fields);

            var result = await _client.Send(
                path: $"{BaseCrudPath}/{HttpUtility.UrlEncode(id)}",
                query: enrichedQuery,
                headers: headers
            );

            return result.ToObject<T>();
        }

        public async Task<T> GetFirstListItem(
            string filter,
            string expand = null,
            string fields = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            var result = await GetList(
                perPage: 1,
                skipTotal: true,
                filter: filter,
                expand: expand,
                fields: fields,
                query: query,
                headers: headers
            );

            if (result.Items.Count == 0)
            {
                throw new ClientException
                (
                    url: _client.BuildUrl($"{BaseCrudPath}/"),
                    statusCode: 404,
                    response: new Dictionary<string, object>
                    {
                        ["code"] = 404,
                        ["message"] = "The requested resource wasn't found.",
                        ["data"] = new()
                    }
                );
            }

            return result.Items.First();
        }

        public async Task<T> Create(
            object body,
            Dictionary<string, object> query = null,
            List<IMultipartFormSection> files = null,
            Dictionary<string, string> headers = null,
            string expand = null,
            string fields = null)
        {
            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAddNonNull("expand", expand);
            enrichedQuery.TryAddNonNull("fields", fields);

            var result = await _client.Send(
                BaseCrudPath,
                method: "POST",
                body: body,
                query: enrichedQuery,
                files: files,
                headers: headers
            );

            return result.ToObject<T>();
        }

        public async virtual Task<T> Update(
            string id,
            object body = null,
            Dictionary<string, object> query = null,
            List<IMultipartFormSection> files = null,
            Dictionary<string, string> headers = null,
            string expand = null,
            string fields = null)
        {
            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAddNonNull("expand", expand);
            enrichedQuery.TryAddNonNull("fields", fields);

            var result = await _client.Send(
                path: $"{BaseCrudPath}/{HttpUtility.UrlEncode(id)}",
                method: "PATCH",
                body: body,
                query: enrichedQuery,
                files: files,
                headers: headers
            );
            
            return result.ToObject<T>();
        }

        public virtual Task Delete(
            string id,
            object body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                path: $"{BaseCrudPath}/{HttpUtility.UrlEncode(id)}",
                method: "DELETE",
                body: body,
                query: query,
                headers: headers
            );
        }
    }
}