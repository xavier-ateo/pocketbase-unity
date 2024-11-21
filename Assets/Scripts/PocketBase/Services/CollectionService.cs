using System.Collections.Generic;
using System.Threading.Tasks;

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
        body ??= new Dictionary<string, object>();
        body.TryAddNonNull("collections", collections);
        body.TryAddNonNull("deleteMissing", deleteMissing);

        return _client.Send<Void>(
            $"{BaseCrudPath}/import",
            method:"PUT",
            body: body,
            query: query,
            headers: headers
        );
    }
}