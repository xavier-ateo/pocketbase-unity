using Newtonsoft.Json;

namespace PocketBaseSdk
{
    public sealed class SseMessage
    {
        public string Id = string.Empty;
        public string Event = "message";
        public string Data = string.Empty;
        public int Retry;

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}