using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocketBaseSdk
{
    /// <summary>
    /// The service that handles the **Log APIs**.
    /// </summary>
    /// <remarks>
    /// Usually shouldn't be initialized manually and instead
    /// <see cref="PocketBase.Logs"/> should be used.
    /// </remarks>
    public sealed class LogService : BaseService
    {
        public LogService(PocketBase client) : base(client)
        {
        }

        /// <summary>
        /// Returns paginated logs list.
        /// </summary>
        public async Task<ResultList<LogModel>> GetList(
            int page = 1,
            int perPage = 30,
            string filter = null,
            string sort = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAddNonNull("page", page);
            enrichedQuery.TryAddNonNull("perPage", perPage);
            enrichedQuery.TryAddNonNull("search", filter);
            enrichedQuery.TryAddNonNull("sort", sort);

            var result = await _client.Send(
                "api/logs",
                query: enrichedQuery,
                headers: headers
            );

            return result.ToObject<ResultList<LogModel>>();
        }
    }
}