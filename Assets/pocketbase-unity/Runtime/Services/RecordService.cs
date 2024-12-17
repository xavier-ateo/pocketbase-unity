using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

namespace PocketBaseSdk
{
    public delegate void RecordSubscriptionFunc<T>(RecordSubscriptionEvent<T> e);

    public class RecordService : BaseCrudService
    {
        public RecordService(PocketBase client, string collectionIdOrName) : base(client)
        {
            _collectionIdOrName = collectionIdOrName;
        }

        private readonly string _collectionIdOrName;

        private string BaseCollectionPath =>
            $"/api/collections/{HttpUtility.UrlEncode(_collectionIdOrName)}";

        protected override string BaseCrudPath =>
            $"{BaseCollectionPath}/records";

        // ---------------------------------------------------------------
        // Realtime handlers
        // ---------------------------------------------------------------

        /// <summary>
        /// Subscribe to realtime changes to the specified <see cref="topic"/> ("*" or record id).
        /// </summary>
        /// <remarks>
        /// If <see cref="topic"/> is the wildcard "*", then this method will subscribe to
        /// any record changes in the collection.
        ///
        /// If <see cref="topic"/> is a record id, then this method will subscribe only
        /// to changes of the specified record id.
        ///
        /// It's OK to subscribe multiple times to the same topic.
        /// </remarks>
        /// <returns>
        /// You can use the returned <see cref="UnsubscribeFunc"/> to remove the subscription.
        /// Or use <see cref="Unsubscribe"/> if you want to remove all
        /// subscriptions attached to the topic.
        /// </returns>
        public Task<UnsubscribeFunc> Subscribe<T>(
            string topic,
            RecordSubscriptionFunc<T> callback,
            string expand = null,
            string filter = null,
            string fields = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Realtime.Subscribe(
                $"{_collectionIdOrName}/{topic}",
                e => callback(JsonConvert.DeserializeObject<RecordSubscriptionEvent<T>>(e.Data)),
                expand,
                filter,
                fields,
                query,
                headers
            );
        }

        /// <summary>
        /// Unsubscribe from all subscriptions of the specified topic
        /// ("*" or record id).
        /// </summary>
        /// <remarks>
        /// If <see cref="topic"/> is the wildcard "*", then this method will unsubscribe
        /// all subscriptions associated to the current collection.
        /// </remarks>
        public Task Unsubscribe(string topic = null)
        {
            return string.IsNullOrEmpty(topic)
                ? _client.Realtime.Unsubscribe($"{_collectionIdOrName}/{topic}")
                : _client.Realtime.UnsubscribeByPrefix(_collectionIdOrName);
        }

        // ---------------------------------------------------------------
        // Post update/delete AuthStore sync
        // ---------------------------------------------------------------

        /// <summary>
        /// Updates a single record model by its id.
        /// </summary>
        /// <remarks>
        /// If the current AuthStore model matches with the updated id, then
        /// on success the client AuthStore will be updated with the result model.
        /// </remarks>
        public override async Task<T> Update<T>(
            string id,
            object body = null,
            Dictionary<string, object> query = null,
            List<IMultipartFormSection> files = null,
            Dictionary<string, string> headers = null,
            string expand = null, string fields = null)
        {
            var item = await base.Update<T>(id, body, query, files, headers, expand, fields);

            if (item is RecordModel record &&
                _client.AuthStore.Model is not null &&
                _client.AuthStore.Model.Id == record.Id)
            {
                _client.AuthStore.Save(_client.AuthStore.Token, record);
            }

            return item;
        }

        /// <summary>
        /// Deletes a single record model by its id.
        /// </summary>
        /// <remarks>
        /// If the current AuthStore model matches with the deleted id,
        /// then on success the client AuthStore will be also cleared.
        /// </remarks>
        public override async Task Delete(
            string id,
            object body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            await base.Delete(id, body, query, headers);

            if (_client.AuthStore.Model is { } model &&
                model.Id == id &&
                new[] { model.CollectionId, model.CollectionName }.Contains(_collectionIdOrName))
            {
                _client.AuthStore.Clear();
            }
        }

        // -----------------------------------------------------------------
        // Auth collection handlers
        // -----------------------------------------------------------------

        /// <summary>
        /// Returns all available application auth methods.
        /// </summary>
        public Task<AuthMethodList> ListAuthMethods(
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                $"{BaseCollectionPath}/auth-methods",
                query: query,
                headers: headers
            ).ContinueWith(t => t.Result.ToObject<AuthMethodList>());
        }

        /// <summary>
        /// Authenticate an auth record by its username/email and password
        /// and returns a new auth token and record data.
        /// </summary>
        /// <remarks>
        /// On success this method automatically updates the client's AuthStore.
        /// </remarks>
        public async Task<RecordAuth> AuthWithPassword(
            string usernameOrEmail,
            string password,
            string expand = null,
            string fields = null,
            Dictionary<string, object> body = null,
            Dictionary<string, string> headers = null,
            Dictionary<string, object> query = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("identity", usernameOrEmail);
            enrichedBody.TryAdd("password", password);

            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAddNonNull("expand", expand);
            enrichedQuery.TryAddNonNull("fields", fields);

            JObject jObj = await _client.Send(
                $"{BaseCollectionPath}/auth-with-password",
                method: "POST",
                body: enrichedBody,
                query: enrichedQuery,
                headers: headers
            );

            var authResult = jObj.ToObject<RecordAuth>();
            
            _client.AuthStore.Save(authResult.Token, authResult.Record);

            return authResult;
        }

        /// <summary>
        /// Sends auth record password reset request.
        /// </summary>
        public Task RequestPasswordReset(
            string email,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("email", email);

            return _client.Send(
                $"{BaseCollectionPath}/request-password-reset",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Confirms auth record password reset request.
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

            return _client.Send(
                $"{BaseCollectionPath}/confirm-password-reset",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Sends auth record verification request.
        /// </summary>
        public Task RequestVerification(
            string email,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("email", email);

            return _client.Send(
                $"{BaseCollectionPath}/request-verification",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Confirms auth record email verification request.
        /// </summary>
        /// <remarks>
        /// On success this method automatically updates the client's AuthStore.
        /// </remarks>
        public async Task ConfirmVerification(
            string verificationToken,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("token", verificationToken);

            await _client.Send(
                $"{BaseCollectionPath}/confirm-verification",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );

            var parts = verificationToken.Split(".");

            if (parts.Length != 3)
            {
                return;
            }

            var payloadPart = parts[1];
            var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                Encoding.UTF8.GetString(Convert.FromBase64String(payloadPart)));

            if (_client.AuthStore.Model is UserModel { Verified: false } userModel &&
                userModel.Id == (string)payload["id"] &&
                userModel.CollectionId == (string)payload["collectionId"])
            {
                userModel.Verified = true;
                _client.AuthStore.Save(_client.AuthStore.Token, userModel);
            }
        }

        /// <summary>
        /// Sends auth record email change request to the provided email.
        /// </summary>
        public Task RequestEmailChange(
            string newEmail,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("newEmail", newEmail);

            return _client.Send(
                $"{BaseCollectionPath}/request-email-change",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Confirms auth record new email address.
        /// </summary>
        /// <remarks>
        /// If the current AuthStore model matches with the record from the token,
        /// then on success the client AuthStore will be also cleared.
        /// </remarks>
        public async Task ConfirmEmailChange(
            string emailChangeToken,
            string userPassword,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("token", emailChangeToken);
            enrichedBody.TryAdd("password", userPassword);

            await _client.Send(
                $"{BaseCollectionPath}/confirm-email-change",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );

            var parts = emailChangeToken.Split(".");

            if (parts.Length != 3)
            {
                return;
            }

            var payloadPart = parts[1];
            var payload = JsonConvert.DeserializeObject<Dictionary<string, object>>(
                Encoding.UTF8.GetString(Convert.FromBase64String(payloadPart)));

            if (_client.AuthStore.Model is { } model &&
                model.Id == (string)payload["id"] &&
                model.CollectionId == (string)payload["collectionId"])
            {
                _client.AuthStore.Clear();
            }
        }

        /// <summary>
        /// Lists all linked external auth providers for the specified record.
        /// </summary>
        public Task<List<ExternalAuthModel>> ListExternalAuths(
            string recordId,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                $"{BaseCollectionPath}/{HttpUtility.UrlEncode(recordId)}/external-auths",
                query: query,
                headers: headers
            ).ContinueWith(t => t.Result.ToObject<List<ExternalAuthModel>>());
        }

        /// <summary>
        /// Unlinks a single external auth provider relation from the
        /// specified record.
        /// </summary>
        public Task UnlinkExternalAuth(
            string recordId,
            string provider,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Send(
                $"{BaseCollectionPath}/{HttpUtility.UrlEncode(recordId)}/external-auths/{HttpUtility.UrlEncode(provider)}",
                method: "DELETE",
                body: body,
                query: query,
                headers: headers
            );
        }

        /// <summary>
        /// Authenticate an auth record with an OAuth2 client provider and returns
        /// a new auth token and record data (including the OAuth2 user profile).
        /// </summary>
        /// <remarks>
        /// On success this method automatically updates the client's AuthStore.
        /// </remarks>
        public async Task<RecordAuth> AuthWithOAuth2Code(
            string provider,
            string code,
            string codeVerifier,
            string redirectUrl,
            Dictionary<string, object> createData = null,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null,
            string expand = null,
            string fields = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("provider", provider);
            enrichedBody.TryAdd("code", code);
            enrichedBody.TryAdd("codeVerifier", codeVerifier);
            enrichedBody.TryAdd("redirectUrl", redirectUrl);
            enrichedBody.TryAdd("createData", createData);

            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAddNonNull("expand", expand);
            enrichedQuery.TryAddNonNull("fields", fields);

            JObject jObj = await _client.Send(
                $"{BaseCollectionPath}/auth-with-oauth2",
                method: "POST",
                body: enrichedBody,
                query: enrichedQuery,
                headers: headers
            );
            
            RecordAuth authResult = jObj.ToObject<RecordAuth>();

            _client.AuthStore.Save(authResult.Token, authResult.Record);

            return authResult;
        }

        /// <summary>
        /// Refreshes the current authenticated auth record instance and
        /// returns a new token and record data.
        /// </summary>
        /// <remarks>
        /// On success this method automatically updates the client's AuthStore.
        /// </remarks>
        public async Task<RecordAuth> AuthRefresh(
            string expand = null,
            string fields = null,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAddNonNull("expand", expand);
            enrichedQuery.TryAddNonNull("fields", fields);

            JObject jObj = await _client.Send(
                $"{BaseCollectionPath}/auth-refresh",
                method: "POST",
                body: body,
                query: enrichedQuery,
                headers: headers
            );
            
            RecordAuth authResult = jObj.ToObject<RecordAuth>();
            
            _client.AuthStore.Save(authResult.Token, authResult.Record);

            return authResult;
        }
    }
}