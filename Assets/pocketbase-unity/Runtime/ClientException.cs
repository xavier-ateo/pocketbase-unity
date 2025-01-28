using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    [Serializable]
    public class ClientException : Exception
    {
        public string URL { get; }
        public int StatusCode { get; }
        public Dictionary<string, object> Response { get; }
        public object OriginalError { get; }

        public ClientException(
            string url = null,
            int statusCode = 0,
            Dictionary<string, object> response = null,
            object originalError = null)
            : base(response?["message"]?.ToString() ?? "Client Exception")
        {
            URL = url;
            StatusCode = statusCode;
            Response = response ?? new();
            OriginalError = originalError;
        }

        private string FormatMessage()
        {
            Dictionary<string, object> data = new()
            {
                ["uri"] = URL,
                ["statusCode"] = StatusCode,
                ["response"] = Response,
                ["originalError"] = OriginalError
            };

            return $"Client Exception: {JsonConvert.SerializeObject(data, Formatting.Indented)}";
        }

        public override string ToString() => FormatMessage();
    }
}