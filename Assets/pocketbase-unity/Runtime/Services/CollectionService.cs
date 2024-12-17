using System.Collections.Generic;
using System.Threading.Tasks;
using Codice.Utils;

namespace PocketBaseSdk
{
    /// <summary>
    /// The service that handles the **Collection APIs**.
    /// </summary>
    /// <remarks>
    /// Usually shouldn't be initialized manually and instead
    /// <see cref="PocketBase.Collections"/> should be used.
    /// </remarks>
    public class CollectionService : BaseCrudService<CollectionModel>
    {
        protected override string BaseCrudPath => "/api/collections";

        public CollectionService(PocketBase client) : base(client)
        {
        }

        /// <summary>
        /// Bulk imports the provided collections. Only admins can perform this action.
        /// </summary>
        /// <remarks>
        /// If <see cref="deleteMissing"/> is set to true, all collections
        /// that are not present in the imported configuration, WILL BE DELETED
        /// (including their related records data)!
        /// </remarks>
        public Task Import(
            List<CollectionModel> collections,
            bool deleteMissing = false,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAddNonNull("collections", collections);
            enrichedBody.TryAddNonNull("deleteMissing", deleteMissing);

            return _client.Send(
                $"{BaseCrudPath}/import",
                method: "PUT",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Returns type indexed map with scaffolded collection models populated with their default field values.
        /// </summary>
        public Task<Dictionary<string, CollectionModel>> GetScaffolds(
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                    $"{BaseCrudPath}/meta/scaffolds",
                    body: body,
                    query: query,
                    headers: headers)
                .ContinueWith(t => t.Result.ToObject<Dictionary<string, CollectionModel>>());
        }

        /// <summary>
        /// Deletes all records associated with the specified collection.
        /// </summary>
        public Task Truncate(
            string collectionIdOrName,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                $"{BaseCrudPath}/{HttpUtility.UrlEncode(collectionIdOrName)}/truncate",
                method: "DELETE",
                body: body,
                query: query,
                headers: headers
            );
        }
    }
}