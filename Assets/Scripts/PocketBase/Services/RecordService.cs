using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

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
        List<MultipartFormFileSection> files = null,
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
        return _client.Send<AuthMethodList>(
            $"{BaseCollectionPath}/auth-methods",
            query: query,
            headers: headers
        );
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
        Dictionary<string, string> body = null,
        Dictionary<string, string> headers = null,
        Dictionary<string, object> query = null)
    {
        body ??= new();
        body.TryAdd("identity", usernameOrEmail);
        body.TryAdd("password", password);

        query ??= new();
        query.TryAddNonNull("expand", expand);
        query.TryAddNonNull("fields", fields);

        var authResult = await _client.Send<RecordAuth>(
            $"{BaseCollectionPath}/auth-with-password",
            method: "POST",
            body: body,
            query: query,
            headers: headers
        );

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
        body ??= new();
        body.TryAdd("email", email);

        return _client.Send<Void>(
            $"{BaseCollectionPath}/request-password-reset",
            method: "POST",
            body: body,
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
        body ??= new();
        body.TryAdd("token", passwordResetToken);
        body.TryAdd("password", password);
        body.TryAdd("passwordConfirm", passwordConfirm);

        return _client.Send<Void>(
            $"{BaseCollectionPath}/confirm-password-reset",
            method: "POST",
            body: body,
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
        body ??= new();
        body.TryAdd("email", email);

        Debug.Log(email);
        Debug.Log(JsonConvert.SerializeObject(body, Formatting.Indented));

        return _client.Send<Void>(
            $"{BaseCollectionPath}/request-verification",
            method: "POST",
            body: body,
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
        body ??= new();
        body.TryAdd("token", verificationToken);

        await _client.Send<Void>(
            $"{BaseCollectionPath}/confirm-verification",
            method: "POST",
            body: body,
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
        body ??= new();
        body.TryAdd("newEmail", newEmail);

        return _client.Send<Void>(
            $"{BaseCollectionPath}/request-email-change",
            method: "POST",
            body: body,
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
        body ??= new();
        body.TryAdd("token", emailChangeToken);
        body.TryAdd("password", userPassword);

        await _client.Send<Void>(
            $"{BaseCollectionPath}/confirm-email-change",
            method: "POST",
            body: body,
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
        return _client.Send<List<ExternalAuthModel>>(
            $"{BaseCollectionPath}/{HttpUtility.UrlEncode(recordId)}/external-auths",
            query: query,
            headers: headers
        );
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
        return _client.Send<Void>(
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
        body ??= new();
        body.TryAdd("provider", provider);
        body.TryAdd("code", code);
        body.TryAdd("codeVerifier", codeVerifier);
        body.TryAdd("redirectUrl", redirectUrl);
        body.TryAdd("createData", createData);

        query ??= new();
        query.TryAddNonNull("expand", expand);
        query.TryAddNonNull("fields", fields);

        var authResult = await _client.Send<RecordAuth>(
            $"{BaseCollectionPath}/auth-with-oauth2",
            method: "POST",
            body: body,
            query: query,
            headers: headers
        );

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
        query ??= new();
        query.TryAddNonNull("expand", expand);
        query.TryAddNonNull("fields", fields);

        var authResult = await _client.Send<RecordAuth>(
            $"{BaseCollectionPath}/auth-refresh",
            method: "POST",
            body: body,
            query: query,
            headers: headers
        );

        _client.AuthStore.Save(authResult.Token, authResult.Record);

        return authResult;
    }
}