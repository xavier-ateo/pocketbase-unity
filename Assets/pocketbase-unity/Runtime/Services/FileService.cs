using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PocketBaseSdk
{
    /// <summary>
    /// The service that handles the File APIs.
    /// </summary>
    /// <remarks>
    /// Usually shouldn't be initialized manually and instead
    /// <see cref="PocketBase.Files"/> should be used.
    /// </remarks>
    public class FileService : BaseService
    {
        public FileService(PocketBase client) : base(client)
        {
        }

        /// <summary>
        /// Builds and returns an absolute URL for the specified file.
        /// </summary>
        public string GetUrl(
            RecordModel record,
            string fileName,
            string thumb = null,
            string token = null,
            bool? download = null,
            Dictionary<string, object> query = null)
        {
            if (string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(record?.Id))
            {
                return string.Empty;
            }

            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAddNonNull("thumb", thumb);
            enrichedQuery.TryAddNonNull("token", token);

            if (download is true)
            {
                enrichedQuery["download"] = string.Empty;
            }

            return _client.BuildUrl(
                $"/api/files/{HttpUtility.UrlEncode(record.CollectionId)}/{HttpUtility.UrlEncode(record.Id)}/{HttpUtility.UrlEncode(fileName)}",
                enrichedQuery
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
            JObject jObj = await _client.Send(
                "/api/files/token",
                method: "POST",
                body: body,
                query: query,
                headers: headers
            );

            FileToken tokenResult = jObj.ToObject<FileToken>();

            return tokenResult.Token;
        }

        private class FileToken
        {
            [JsonProperty("token")]
            public string Token { get; set; }
        }
    }
}