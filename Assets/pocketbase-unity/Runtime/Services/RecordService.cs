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
    public delegate void RecordSubscriptionFunc(RecordSubscriptionEvent e);

    public delegate void OAuth2UrlCallbackFunc(Uri url);

    public class RecordService : BaseCrudService<RecordModel>
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
        public Task<UnsubscribeFunc> Subscribe(
            string topic,
            RecordSubscriptionFunc callback,
            string expand = null,
            string filter = null,
            string fields = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            return _client.Realtime.Subscribe(
                $"{_collectionIdOrName}/{topic}",
                e => callback(JsonConvert.DeserializeObject<RecordSubscriptionEvent>(e.Data)),
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
        public override async Task<RecordModel> Update(
            string id,
            object body = null,
            Dictionary<string, object> query = null,
            List<IMultipartFormSection> files = null,
            Dictionary<string, string> headers = null,
            string expand = null,
            string fields = null)
        {
            RecordModel item = await base.Update(id, body, query, files, headers, expand, fields);

            // Check if the current AuthStore record matches the updated item
            if (_client.AuthStore.Record is not null &&
                _client.AuthStore.Record.Id == item.Id &&
                new[]
                {
                    _client.AuthStore.Record.CollectionId,
                    _client.AuthStore.Record.CollectionName
                }.Contains(_collectionIdOrName))
            {
                // Merge the response fields with the current auth record
                var currentExpand = _client.AuthStore.Record["expand"]?.ToObject<Dictionary<string, object>>() ??
                                    new();

                var newExpand = item["expand"]?.ToObject<Dictionary<string, object>>() ?? new();

                var data = _client.AuthStore.Record.Data.ToObject<Dictionary<string, object>>();

                // Merge with the new item data
                foreach (var (key, value) in item.Data)
                {
                    data[key] = value;
                }

                if (currentExpand.Any())
                {
                    foreach (var (key, value) in newExpand)
                    {
                        currentExpand[key] = value;
                    }

                    data["expand"] = currentExpand;
                }

                _client.AuthStore.Save(_client.AuthStore.Token, RecordModel.Create(data));
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

            if (_client.AuthStore.Record is { } model &&
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
        public async Task<AuthMethodsList> ListAuthMethods(
            string fields = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedQuery = new(query ?? new())
            {
                ["fields"] = "mfa,otp,password,oauth2"
            };

            var result = await _client.Send(
                $"{BaseCollectionPath}/auth-methods",
                query: enrichedQuery,
                headers: headers
            );

            return result.ToObject<AuthMethodsList>();
        }

        /// <summary>
        /// Authenticate an auth record by its username/email and password
        /// and returns a new auth token and record data.
        /// </summary>
        /// <remarks>
        /// On success, this method automatically updates the client's AuthStore.
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

            var jObj = await _client.Send(
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
        /// On success, this method automatically updates the client's AuthStore.
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

            if (_client.AuthStore.Record is { } record &&
                (bool)record["verified"] is false &&
                record.Id == (string)payload["id"] &&
                record.CollectionId == (string)payload["collectionId"])
            {
                record["verified"] = true;
                _client.AuthStore.Save(_client.AuthStore.Token, record);
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

            if (_client.AuthStore.Record is { } model &&
                model.Id == (string)payload["id"] &&
                model.CollectionId == (string)payload["collectionId"])
            {
                _client.AuthStore.Clear();
            }
        }

        /// <summary>
        /// Authenticate an auth record with an OAuth2 client provider and returns
        /// a new auth token and record data (including the OAuth2 user profile).
        /// </summary>
        /// <remarks>
        /// On success, this method automatically updates the client's AuthStore.
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

            var jObj = await _client.Send(
                $"{BaseCollectionPath}/auth-with-oauth2",
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
        /// Authenticate a single auth collection record with OAuth2 without custom redirects,
        /// deep-links or even page reload.
        /// </summary>
        /// 
        /// <remarks>
        /// <para>
        /// This method initializes a one-off realtime subscription
        /// and will call <see cref="urlCallback"/> with the OAuth2 vendor url to authenticate.
        /// Once the external OAuth2 sign-in/sign-up flow is completed,
        /// the popup window will be automatically closed and the OAuth2 data sent back
        /// to the user through the previously established realtime connection.
        /// </para>
        /// <para>
        /// On success, this method automatically updates the client's AuthStore.
        /// </para>
        /// <para>
        /// Note: when creating the OAuth2 app in the provider dashboard,
        /// you have to configure `https://yourdomain.com/api/oauth2-redirect` as redirectURL.
        /// </para>
        /// </remarks>
        /// <example> 
        /// <code>
        /// await pb.collection("users").authWithOAuth2("google", url => {
        ///   Application.OpenUrl(url);
        /// });
        /// </code> 
        /// </example>
        public Task<RecordAuth> AuthWithOAuth2(
            string providerName,
            OAuth2UrlCallbackFunc urlCallback,
            List<string> scopes = null,
            Dictionary<string, object> createData = null,
            string expand = null,
            string fields = null)
        {
            TaskCompletionSource<RecordAuth> completer = new();

            MainThreadDispatcher.Instance.Enqueue(async () =>
            {
                UnsubscribeFunc unsubscribeFunc = null;

                try
                {
                    var authMethods = await ListAuthMethods();

                    var provider = authMethods.OAuth2.Providers.FirstOrDefault(p => p.Name == providerName)
                                   ?? throw new ClientException(originalError: $"Missing provider {providerName}");

                    var redirectUrl = _client.BuildUrl("/api/oauth2-redirect");

                    unsubscribeFunc = await _client.Realtime.Subscribe(
                        "@oauth2",
                        e => HandleMessage(e, unsubscribeFunc),
                        expand: expand,
                        fields: fields
                    );

                    Uri authUrl = new(provider.AuthUrl + redirectUrl);
                    var queryParameters = HttpUtility.ParseQueryString(authUrl.Query);
                    queryParameters["state"] = _client.Realtime.ClientId;

                    if (scopes?.Any() is true)
                    {
                        queryParameters["scope"] = string.Join(" ", scopes);
                    }

                    var builder = new UriBuilder(authUrl) { Query = queryParameters.ToString() };
                    var finalAuthUrl = builder.Uri;

                    urlCallback(finalAuthUrl);

                    async void HandleMessage(SseMessage e, UnsubscribeFunc unsubFunc)
                    {
                        try
                        {
                            string oldState = _client.Realtime.ClientId;

                            var eventData = JObject.Parse(e.Data);
                            string code = (string)eventData["code"] ?? "";
                            string state = (string)eventData["state"] ?? "";
                            string error = (string)eventData["error"] ?? "";

                            if (string.IsNullOrEmpty(state) || state != oldState)
                                throw new InvalidOperationException("State parameters don't match.");

                            if (!string.IsNullOrEmpty(error) || string.IsNullOrEmpty(code))
                                throw new InvalidOperationException("OAuth2 redirect error or missing code.");

                            var auth = await AuthWithOAuth2Code(
                                provider.Name,
                                code,
                                provider.CodeVerifier,
                                redirectUrl.ToString(),
                                createData,
                                expand: expand,
                                fields: fields
                            );

                            completer.TrySetResult(auth);
                            unsubFunc?.Invoke();
                        }
                        catch (Exception ex)
                        {
                            completer.TrySetException(new ClientException(originalError: ex));
                        }
                    }
                }
                catch (Exception ex)
                {
                    completer.TrySetException(new ClientException(originalError: ex));
                    unsubscribeFunc?.Invoke();
                }
            });

            return completer.Task;
        }

        /// <summary>
        /// Sends auth record OTP request to the provided email.
        /// </summary>
        public async Task<OTPResponse> RequestOTP(
            string email,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());

            if (!string.IsNullOrEmpty(email))
                enrichedBody.TryAdd("email", email);

            var result = await _client.Send(
                $"{BaseCollectionPath}/request-otp",
                method: "POST",
                body: enrichedBody,
                query: query,
                headers: headers
            );

            return result.ToObject<OTPResponse>();
        }

        /// <summary>
        /// Authenticate an auth record via OTP.
        /// </summary>
        /// <remarks>
        /// On success, this method automatically updates the client's AuthStore.
        /// </remarks>
        public async Task<RecordAuth> AuthWithOTP(
            string otpId,
            string password,
            string expand = null,
            string fields = null,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());

            if (!string.IsNullOrEmpty(otpId))
                enrichedBody.TryAdd(nameof(otpId), otpId);

            if (!string.IsNullOrEmpty(password))
                enrichedBody.TryAdd(nameof(password), password);

            Dictionary<string, object> enrichedQuery = new(query ?? new());

            if (!string.IsNullOrEmpty(expand))
                enrichedQuery.TryAdd(nameof(expand), expand);

            if (!string.IsNullOrEmpty(fields))
                enrichedQuery.TryAdd(nameof(fields), fields);

            var result = await _client.Send(
                $"{BaseCollectionPath}/auth-with-otp",
                method: "POST",
                body: enrichedBody,
                query: enrichedQuery,
                headers: headers
            );

            return result.ToObject<RecordAuth>();
        }

        /// <summary>
        /// Impersonate authenticates with the specified recordId and
        /// returns a new client with the received auth token in a memory store.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If `duration` is 0, the generated auth token will fall back
        /// to the default collection auth token duration.
        /// </para>
        /// <para>
        /// This action currently requires superuser's privileges.
        /// </para>
        /// </remarks>
        public async Task<PocketBase> Impersonate(
            string recordId,
            float duration,
            string expand = null,
            string fields = null,
            Dictionary<string, object> body = null,
            Dictionary<string, object> query = null,
            Dictionary<string, string> headers = null)
        {
            Dictionary<string, object> enrichedBody = new(body ?? new());
            enrichedBody.TryAdd("duration", duration);

            Dictionary<string, object> enrichedQuery = new(query ?? new());
            enrichedQuery.TryAddNonNull("expand", expand);
            enrichedQuery.TryAddNonNull("fields", fields);

            Dictionary<string, string> enrichedHeaders = new(headers ?? new());
            enrichedHeaders.TryAddNonNull("Authorization", _client.AuthStore.Token);

            // Create a new client with the impersonated auth state
            // ---
            PocketBase tempClient = new PocketBase(
                _client.BaseUrl,
                lang: _client.Lang
            );

            var result = await tempClient.Send(
                $"{BaseCollectionPath}/impersonate/{HttpUtility.UrlEncode(recordId)}",
                method: "POST",
                body: enrichedBody,
                query: enrichedQuery,
                headers: enrichedHeaders
            );

            RecordAuth authData = result.ToObject<RecordAuth>();

            tempClient.AuthStore.Save(authData.Token, authData.Record);
            // ---

            return tempClient;
        }

        /// <summary>
        /// Refreshes the current authenticated auth record instance and
        /// returns a new token and record data.
        /// </summary>
        /// <remarks>
        /// On success, this method automatically updates the client's AuthStore.
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

            var jObj = await _client.Send(
                $"{BaseCollectionPath}/auth-refresh",
                method: "POST",
                body: body,
                query: enrichedQuery,
                headers: headers
            );

            var authResult = jObj.ToObject<RecordAuth>();

            _client.AuthStore.Save(authResult.Token, authResult.Record);

            return authResult;
        }
    }
}