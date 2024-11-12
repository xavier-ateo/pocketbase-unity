using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using UnityEngine.Networking;

public abstract class BaseCrudService : BaseService
{
    protected abstract string BaseCrudPath { get; }

    private static readonly IReadOnlyDictionary<string, object> EmptyData = new Dictionary<string, object>();

    protected BaseCrudService(PocketBase client) : base(client)
    {
    }

    public Task<List<T>> GetFullList<T>(
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
            var list = await GetList<T>(
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

    public Task<ResultList<T>> GetList<T>(
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
        query ??= new();

        Dictionary<string, object> enrichedQuery = new(query);
        enrichedQuery.TryAdd("page", page);
        enrichedQuery.TryAdd("perPage", perPage);
        enrichedQuery.TryAdd("skipTotal", skipTotal);
        enrichedQuery.TryAddNonNull("expand", expand);
        enrichedQuery.TryAddNonNull("filter", filter);
        enrichedQuery.TryAddNonNull("sort", sort);
        enrichedQuery.TryAddNonNull("fields", fields);

        return _client.Send<ResultList<T>>(
            BaseCrudPath,
            query: enrichedQuery,
            headers: headers
        );
    }

    public Task<T> GetOne<T>(
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
                    ["data"] = EmptyData
                }
            );
        }

        query ??= new();

        Dictionary<string, object> enrichedQuery = new(query);
        enrichedQuery.TryAddNonNull("expand", expand);
        enrichedQuery.TryAddNonNull("fields", fields);

        return _client.Send<T>(
            path: $"{BaseCrudPath}/{HttpUtility.UrlEncode(id)}",
            query: enrichedQuery,
            headers: headers
        );
    }

    public async Task<T> GetFirstListItem<T>(
        string filter,
        string expand = null,
        string fields = null,
        Dictionary<string, object> query = null,
        Dictionary<string, string> headers = null)
    {
        var result = await GetList<T>(
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
                    ["data"] = EmptyData
                }
            );
        }

        return result.Items.First();
    }

    public Task<T> Create<T>(
        object body,
        Dictionary<string, object> query = null,
        List<MultipartFormFileSection> files = null,
        Dictionary<string, string> headers = null,
        string expand = null,
        string fields = null)
    {
        query ??= new();

        Dictionary<string, object> enrichedQuery = new(query);
        enrichedQuery.TryAddNonNull("expand", expand);
        enrichedQuery.TryAddNonNull("fields", fields);

        return _client.Send<T>(
            BaseCrudPath,
            method: "POST",
            body: body,
            query: enrichedQuery,
            files: files,
            headers: headers
        );
    }

    public virtual Task<T> Update<T>(
        string id,
        object body = null,
        Dictionary<string, object> query = null,
        List<MultipartFormFileSection> files = null,
        Dictionary<string, string> headers = null,
        string expand = null,
        string fields = null)
    {
        query ??= new();

        Dictionary<string, object> enrichedQuery = new(query);
        enrichedQuery.TryAddNonNull("expand", expand);
        enrichedQuery.TryAddNonNull("fields", fields);

        return _client.Send<T>(
            path: $"{BaseCrudPath}/{HttpUtility.UrlEncode(id)}",
            method: "PATCH",
            body: body,
            query: enrichedQuery,
            files: files,
            headers: headers
        );
    }

    public virtual Task Delete(
        string id,
        object body = null,
        Dictionary<string, object> query = null,
        Dictionary<string, string> headers = null)
    {
        return _client.Send<Void>(
            path: $"{BaseCrudPath}/{HttpUtility.UrlEncode(id)}",
            method: "DELETE",
            body: body,
            query: query,
            headers: headers
        );
    }
}