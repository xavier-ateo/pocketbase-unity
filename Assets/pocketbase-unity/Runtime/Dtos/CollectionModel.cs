using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    /// <summary>
    /// Response DTO of a single collection model.
    /// </summary>
    [Serializable]
    public sealed class CollectionModel
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("type")]
        public string Type { get; set; } = "base";

        [JsonProperty("created")]
        public string Created { get; set; } = string.Empty;

        [JsonProperty("updated")]
        public string Updated { get; set; } = string.Empty;

        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("system")]
        public bool System { get; set; }

        [JsonProperty("listRule")]
        public string ListRule { get; set; }

        [JsonProperty("viewRule")]
        public string ViewRule { get; set; }

        [JsonProperty("createRule")]
        public string CreateRule { get; set; }

        [JsonProperty("updateRule")]
        public string UpdateRule { get; set; }

        [JsonProperty("deleteRule")]
        public string DeleteRule { get; set; }

        [JsonProperty("schema")]
        public List<CollectionField> Fields { get; set; } = new();

        [JsonProperty("indexes")]
        public List<string> Indexes { get; set; } = new();

        // View fields

        [JsonProperty("viewQuery")]
        public string ViewQuery { get; set; }

        // Auth fields

        [JsonProperty("authRule")]
        public string AuthRule { get; set; }

        [JsonProperty("manageRule")]
        public string ManageRule { get; set; }

        [JsonProperty("authAlert")]
        public AuthAlertConfig AuthAlert { get; set; }

        [JsonProperty("oauth2")]
        public OAuth2Config OAuth2 { get; set; }

        [JsonProperty("passwordAuth")]
        public PasswordAuthConfig PasswordAuth { get; set; }

        [JsonProperty("mfa")]
        public MFAConfig MFA { get; set; }

        [JsonProperty("otp")]
        public OTPConfig OTP { get; set; }

        [JsonProperty("authToken")]
        public TokenConfig AuthToken { get; set; }

        [JsonProperty("passwordResetToken")]
        public TokenConfig PasswordResetToken { get; set; }

        [JsonProperty("emailChangeToken")]
        public TokenConfig EmailChangeToken { get; set; }

        [JsonProperty("verificationToken")]
        public TokenConfig VerificationToken { get; set; }

        [JsonProperty("fileToken")]
        public TokenConfig FileToken { get; set; }

        [JsonProperty("verificationTemplate")]
        public EmailTemplateConfig VerificationTemplate { get; set; }

        [JsonProperty("resetPasswordTemplate")]
        public EmailTemplateConfig ResetPasswordTemplate { get; set; }

        [JsonProperty("confirmEmailChangeTemplate")]
        public EmailTemplateConfig ConfirmEmailChangeTemplate { get; set; }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}