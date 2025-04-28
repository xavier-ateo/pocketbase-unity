using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;
using UnityEngine.Networking;

namespace PocketBaseSdk
{
    /// <summary>
    /// The service that handles the Backup and restore APIs.
    /// </summary>
    /// <remarks>
    /// Usually shouldn't be initialized manually and instead
    /// <see cref="PocketBase.Backups"/> should be used.
    /// </remarks>
    public class BackupService : BaseService
    {
        public BackupService(PocketBase client) : base(client)
        {
        }

        /// <summary>
        /// Fetch all available app settings.
        /// </summary>
        public async Task<List<BackupFileInfo>> GetFullList(
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            var result = await _client.Send(
                "/api/backups",
                query: query,
                headers: headers
            );

            return result.ToObject<List<BackupFileInfo>>();
        }

        public Task Create(
            string baseName,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());

            if (!string.IsNullOrEmpty(baseName))
                enrichedBody.TryAdd("baseName", baseName);

            return _client.Send(
                "/api/backups",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Uploads an existing backup file.
        /// </summary>
        /// <remarks>
        /// The key of the MultipartFile file must be "file".
        /// </remarks>
        public Task Upload(
            IMultipartFormSection file,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                "/api/backups/upload",
                method: "POST",
                body: body,
                query: query,
                headers: headers,
                files: new() { file }
            );
        }

        /// <summary>
        /// Deletes a single backup file.
        /// </summary>
        public Task Delete(
            string key,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                $"/api/backups/{HttpUtility.UrlEncode(key)}",
                method: "DELETE",
                body: body,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Initializes an app data restore from an existing backup.
        /// </summary>
        public Task Restore(
            string key,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                $"/api/backups/{HttpUtility.UrlEncode(key)}/restore",
                method: "POST",
                body: body,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Builds a download url for a single existing backup using a
        /// superuser file token and the backup file key.
        /// </summary>
        /// <remarks>
        /// The file token can be generated via <see cref="FileService.GetToken"/>.
        /// </remarks>
        public string GetDownloadUrl(
            string token,
            string key,
            Dictionary<string, object> query = null)
        {
            Dictionary<string, object> parameters = new(query ?? new());
            parameters.TryAdd(nameof(token), token);

            return _client.BuildUrl(
                $"/api/backups/{HttpUtility.UrlEncode(key)}",
                parameters
            );
        }
    }
}