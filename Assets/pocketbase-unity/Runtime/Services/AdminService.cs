using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Networking;

namespace PocketBaseSdk
{
    public class AdminService : BaseCrudService<AdminModel>
    {
        protected override string BaseCrudPath => "/api/admins";

        /// <summary>
        /// The service that handles the Admin APIs.
        /// </summary>
        /// <remarks>
        /// Usually shouldn't be initialized manually and instead
        /// <see cref="PocketBase.Admins"/> should be used.
        /// </remarks>
        public AdminService(PocketBase client) : base(client)
        {
        }

        // ---------------------------------------------------------------
        // Post update/delete AuthStore sync
        // ---------------------------------------------------------------

        /// <summary>
        /// Updates a single admin model by its id.
        /// </summary>
        /// <remarks>
        /// If the current AuthStore model matches with the updated id, then
        /// on success the client AuthStore will be updated with the result model.
        /// </remarks>
        public new async Task<AdminModel> Update(
            string id,
            object body = null,
            Dictionary<string, object> query = null,
            List<IMultipartFormSection> files = null,
            Dictionary<string, string> headers = null,
            string expand = null,
            string fields = null)
        {
            var item = await base.Update<AdminModel>(id, body, query, files, headers, expand, fields);

            if (item is { } record &&
                _client.AuthStore.Model is { } authModel &&
                authModel.Id == record.Id)
            {
                _client.AuthStore.Save(_client.AuthStore.Token, record);
            }

            return item;
        }

        /// <summary>
        /// Deletes a single admin model by its id.
        /// </summary>
        /// <remarks>
        /// If the current AuthStore model matches with the deleted id,
        /// then on success the client AuthStore will be also cleared.
        /// </remarks>
        public new async Task Delete(
            string id,
            object body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            await base.Delete(id, body, query, headers);

            if (_client.AuthStore.Model is AdminModel adminModel &&
                adminModel.Id == id)
            {
                _client.AuthStore.Clear();
            }
        }

        // -----------------------------------------------------------------
        // Auth collection handlers
        // -----------------------------------------------------------------

        /// <summary>
        /// Authenticate an admin account by its email and password
        /// and returns a new auth token and admin data.
        /// </summary>
        /// <remarks>
        /// On success this method automatically updates the client's AuthStore.
        /// </remarks>
        public async Task<AdminAuth> AuthWithPassword(
            string email,
            string password,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("email", email);
            enrichedBody.TryAdd("password", password);

            var authResult = await _client.Send<AdminAuth>(
                $"{BaseCrudPath}/auth-with-password",
                method: "POST",
                body: enrichedBody,
                query: enrichedBody,
                headers: headers
            );

            _client.AuthStore.Save(authResult.Token, authResult.Admin);

            return authResult;
        }

        /// <summary>
        /// Refreshes the current admin authenticated instance and
        /// returns a new auth token and admin data.
        /// </summary>
        /// <remarks>
        /// On success this method automatically updates the client's AuthStore.
        /// </remarks>
        public async Task<AdminAuth> Refresh(
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            var authResult = await _client.Send<AdminAuth>(
                $"{BaseCrudPath}/auth-refresh",
                method: "POST",
                body: body,
                query: query,
                headers: headers
            );

            _client.AuthStore.Save(authResult.Token, authResult.Admin);

            return authResult;
        }

        /// <summary>
        /// Sends admin password reset request.
        /// </summary>
        public Task RequestPasswordReset(
            string email,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("email", email);

            return _client.Send<Void>(
                $"{BaseCrudPath}/request-password-reset",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Confirms admin password reset request.
        /// </summary>
        public Task ConfirmPasswordReset(
            string passwordResetToken,
            string password,
            string passwordConfirm,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("token", passwordResetToken);
            enrichedBody.TryAdd("password", password);
            enrichedBody.TryAdd("passwordConfirm", passwordConfirm);

            return _client.Send<Void>(
                $"{BaseCrudPath}/confirm-password-reset",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }
    }
}