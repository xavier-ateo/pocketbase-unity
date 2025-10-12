using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace PocketBaseSdk
{
    public record AuthStoreEvent(string Token, RecordModel Record)
    {
        public string Token { get; } = Token;
        public RecordModel Record { get; } = Record;

        public override string ToString() => $"token: {Token}\nRecord: {Record})";
    }

    [Serializable]
    public class AuthStore
    {
        public readonly EventStream<AuthStoreEvent> OnChange = new();

        public string Token { get; private set; }
        public RecordModel Record { get; private set; }

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(Token))
            {
                return false;
            }

            var parts = Token.Split(".");

            if (parts.Length != 3)
            {
                return false;
            }

            // Add padding if necessary
            var tokenPart = parts[1];
            switch (tokenPart.Length % 4)
            {
                case 2:
                    tokenPart += "==";
                    break;
                case 3:
                    tokenPart += "=";
                    break;
            }

            try
            {
                var jsonBytes = Convert.FromBase64String(tokenPart);
                var jsonString = Encoding.UTF8.GetString(jsonBytes);
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);

                // Check if data is null or doesn't contain the "exp" key
                if (data?.TryGetValue("exp", out var expValue) != true)
                {
                    return false;
                }

                long exp;
                if (expValue is long longExp)
                {
                    exp = longExp;
                }
                else if (!long.TryParse(expValue?.ToString(), out exp))
                {
                    exp = 0;
                }

                return exp > DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Saves the provided <paramref name="newToken"/> and <paramref name="newRecord"/> auth data into the store.
        /// </summary> 
        public virtual void Save(string newToken, RecordModel newRecord)
        {
            Token = newToken;
            Record = newRecord;

            OnChange.Invoke(new AuthStoreEvent(Token, Record));
        }

        /// <summary>
        /// Clears the previously stored <see cref="Token"/> and <see cref="Record"/> auth data.
        /// </summary>
        public virtual void Clear()
        {
            Token = string.Empty;
            Record = null;

            OnChange.Invoke(new AuthStoreEvent(Token, Record));
        }
    }
}