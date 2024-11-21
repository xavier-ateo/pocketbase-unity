using System.Collections.Generic;
using System.Threading.Tasks;

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
    public Task<Dictionary<string, object>> GetAll(
        Dictionary<string, object> query = null,
        Dictionary<string, string> headers = null)
    {
        return _client.Send<Dictionary<string, object>>(
            "/api/settings",
            query: query,
            headers: headers
        );
    }
    
    /// <summary>
    /// Bulk update app settings.
    /// </summary>
    public Task<Dictionary<string, object>> Update(
        Dictionary<string, object> body,
        Dictionary<string, object> query = null,
        Dictionary<string, string> headers = null)
    {
        return _client.Send<Dictionary<string, object>>(
            "/api/settings",
            method: "PATCH",
            body: body,
            query: query,
            headers: headers
        );
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
        body ??= new();
        body.TryAddNonNull("filesystem", filesystem);

        return _client.Send<Void>(
            "/api/settings/test/s3",
            method: "POST",
            body: body,
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
        Dictionary<string, object> body = null,
        Dictionary<string, object> query = null,
        Dictionary<string, string> headers = null)
    {
        body ??= new();
        body.TryAddNonNull("email", toEmail);
        body.TryAddNonNull("template", template);

        return _client.Send<Void>(
            "/api/settings/test/email",
            method: "POST",
            body: body,
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
        body ??= new();
        body.TryAddNonNull("clientId", clientId);
        body.TryAddNonNull("teamId", teamId);
        body.TryAddNonNull("keyId", keyId);
        body.TryAddNonNull("privateKey", privateKey);
        body.TryAddNonNull("duration", duration);

        return _client.Send<AppleClientSecret>(
            "/api/settings/apple/generate-client-secret",
            method: "POST",
            body: body,
            query: query,
            headers: headers
        );
    }
}