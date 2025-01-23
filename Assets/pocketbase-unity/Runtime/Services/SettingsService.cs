using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace PocketBaseSdk
{
    /// <summary>
    /// The service that handles the **Settings APIs**.
    /// </summary>
    /// <remarks>
    /// Usually shouldn't be initialized manually and instead
    /// <see cref="PocketBase.Settings"/> should be used.
    /// </remarks>
    public class SettingsService : BaseService
    {
        public SettingsService(PocketBase client) : base(client)
        {
        }

        /// <summary>
        /// Fetch all available app settings.
        /// </summary>
        public Task<JObject> GetAll(
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                "/api/settings",
                query: query,
                headers: headers
            ).ContinueWith(t => t.Result.ToObject<Dictionary<string, object>>());
        }

        /// <summary>
        /// Bulk update app settings.
        /// </summary>
        public Task<JObject> Update(
            Dictionary<string, object> body,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                "/api/settings",
                method: "PATCH",
                body: body,
                query: query,
                headers: headers
            ).ContinueWith(t => t.Result.ToObject<Dictionary<string, object>>());
        }

        /// <summary>
        /// Perform a S3 storage connection test.
        /// </summary>
        public Task TestS3(
            string filesystem = "storage",
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAddNonNull("filesystem", filesystem);

            return _client.Send(
                "/api/settings/test/s3",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Sends a test email.
        /// </summary>
        /// <remarks>
        /// The possible <see cref="template"/> values are:
        /// - verification
        /// - password-reset
        /// - email-change
        /// </remarks>
        public Task TestEmail(
            string toEmail,
            string template,
            string collection, // fallback to _superusers
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAddNonNull("email", toEmail);
            enrichedBody.TryAddNonNull("template", template);
            enrichedBody.TryAdd("collection", collection);

            return _client.Send(
                "/api/settings/test/email",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Generates a new Apple OAuth2 client secret.
        /// </summary>
        public Task<AppleClientSecret> GenerateAppleClientSecret(
            string clientId,
            string teamId,
            string keyId,
            string privateKey,
            int duration,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAddNonNull("clientId", clientId);
            enrichedBody.TryAddNonNull("teamId", teamId);
            enrichedBody.TryAddNonNull("keyId", keyId);
            enrichedBody.TryAddNonNull("privateKey", privateKey);
            enrichedBody.TryAddNonNull("duration", duration);

            return _client.Send(
                "/api/settings/apple/generate-client-secret",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            ).ContinueWith(t => t.Result.ToObject<AppleClientSecret>());
        }
    }
}