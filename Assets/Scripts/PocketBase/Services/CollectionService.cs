using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class CollectionService
{
    private readonly PocketBase _pocketBase;

    public CollectionService(PocketBase pocketBase)
    {
        _pocketBase = pocketBase;
    }

    public async Task<T> GetOne<T>(string id, CollectionFilter filter = null)
    {
        return await Task.FromResult(default(T));
    }
}