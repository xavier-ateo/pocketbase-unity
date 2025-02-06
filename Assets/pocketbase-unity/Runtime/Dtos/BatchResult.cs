using Newtonsoft.Json;

namespace PocketBaseSdk
{
    public class BatchResult
    {
        [JsonProperty("status")]
        public int Status { get; set; }

        // usually null, Dictionary<string, object> or List<Dictionary<string, object>>
        [JsonProperty("body")]
        public object Body { get; set; }

        public BatchResult(int status, object body)
        {
            Status = status;
            Body = body;
        }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}