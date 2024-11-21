using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

/// <summary>
/// The service that handles the File APIs.
/// </summary>
/// <remarks>
/// Usually shouldn't be initialized manually and instead
/// <see cref="PocketBase.Files"/> should be used.
/// </remarks>
public class FileService : BaseService
{
    public static readonly Uri EmptyUri = new Uri("about:blank", UriKind.Absolute);

    public FileService(PocketBase client) : base(client)
    {
    }

    /// <summary>
    /// Builds and returns an absolute URL for the specified file.
    /// </summary>
    public Uri GetUrl(
        RecordModel record,
        string fileName,
        string thumb = null,
        string token = null,
        bool? download = null,
        Dictionary<string, object> query = null)
    {
        if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(record?.Id))
        {
            return EmptyUri;
        }

        query ??= new();
        query.TryAddNonNull("thumb", thumb);
        query.TryAddNonNull("token", token);

        if (download is true)
        {
            query["download"] = string.Empty;
        }

        return _client.BuildUrl(
            $"/api/files/{HttpUtility.UrlEncode(record.CollectionId)}/{HttpUtility.UrlEncode(record.Id)}/{HttpUtility.UrlEncode(fileName)}",
            query
        );
    }

    /// <summary>
    /// Requests a new private file access token for the current auth model.
    /// </summary>
    public async Task<string> GetToken(
        Dictionary<string, object> body = null,
        Dictionary<string, object> query = null,
        Dictionary<string, string> headers = null)
    {
        var tokenResult = await _client.Send<FileToken>(
            "api/files/token",
            method: "POST",
            body: body,
            query: query,
            headers: headers
        );
        
        return tokenResult.Token;
    }

    private class FileToken
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }
}

