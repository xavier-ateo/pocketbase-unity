using System.Collections.Generic;
using System.Threading.Tasks;

namespace PocketBaseSdk
{
    public class HealthService : BaseService
    {
        public HealthService(PocketBase client) : base(client)
        {
        }

        /// <summary>
        /// The service that handles the Health APIs.
        /// </summary>
        /// <remarks>
        /// Usually shouldn't be initialized manually and instead
        /// <cref cref="PocketBase.Health"/> should be used.
        /// </remarks>
        public async Task<HealthCheck> Check(
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            var result = await _client.Send(
                "/api/health",
                query: query,
                headers: headers
            );

            return result.ToObject<HealthCheck>();
        }
    }
}