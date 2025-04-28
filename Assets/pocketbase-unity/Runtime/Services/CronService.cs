using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace PocketBaseSdk
{
    /// <summary>
    /// The service that handles the Cron APIs.
    /// </summary>
    /// <remarks>
    /// Usually shouldn't be initialized manually and instead
    /// <see cref="PocketBase.Backups"/> should be used.
    /// </remarks>
    public class CronService : BaseService
    {
        public CronService(PocketBase client) : base(client)
        {
        }

        /// <summary>
        /// Returns a list with all registered app cron jobs.
        /// </summary>
        public async Task<List<CronJob>> GetFullList(
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            var result = await _client.Send(
                "/api/crons",
                query: query,
                headers: headers
            );

            return result.ToObject<List<CronJob>>();
        }

        /// <summary>
        /// Runs the specified cron job.
        /// </summary>
        public Task Run(
            string jobId,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                $"/api/crons/{HttpUtility.UrlEncode(jobId)}",
                method: "POST",
                body: body,
                query: query,
                headers: headers
            );
        }
    }
}