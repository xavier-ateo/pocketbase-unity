namespace PocketBaseSdk
{
    public abstract class BaseService
    {
        protected readonly PocketBase _client;

        protected BaseService(PocketBase client)
        {
            _client = client;
        }
    }
}