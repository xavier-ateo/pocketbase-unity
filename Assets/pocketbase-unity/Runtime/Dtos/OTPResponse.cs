using System;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of a otp request response.
    /// </summary>
    [Serializable]
    public class OTPResponse
    {
        [JsonProperty("otpId")]
        public string OtpId { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}