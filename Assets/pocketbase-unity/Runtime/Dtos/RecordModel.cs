using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PocketBaseSdk
{
    [Serializable]
    public class RecordModel
    {
        [JsonIgnore]
        public string Id
        {
            get => GetStringValue("id", "");
            set => Set("id", value);
        }

        [JsonIgnore]
        public string CollectionId => GetStringValue("collectionId");

        [JsonIgnore]
        public string CollectionName => GetStringValue("collectionName");

        [Obsolete("Created is no longer mandatory field; use Get<string>(\"created\")")]
        [JsonIgnore]
        public DateTime? Created => Get<DateTime>("created");

        [Obsolete("Updated is no longer mandatory field; use Get<string>(\"updated\")")]
        [JsonIgnore]
        public DateTime? Updated => Get<DateTime>("updated");

        [JsonExtensionData]
        private IDictionary<string, JToken> _data;

        [JsonIgnore]
        public JObject Data => new JObject(_data);

        public static RecordModel Create(Dictionary<string, object> data)
        {
            var json = JsonConvert.SerializeObject(data);
            return JsonConvert.DeserializeObject<RecordModel>(json);
        }

        /// <summary>
        /// Extracts a single model value by a dot-notation path
        /// and tries to cast it to the specified generic type.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If explicitly set, returns <paramref name="defaultValue"/> on missing path.
        /// </para>
        /// <para>
        /// For more details about the casting rules, please refer to
        /// <see cref="Caster.Extract"/>.
        /// </para>
        /// </remarks>
        public T Get<T>(string fieldNameOrPath, T defaultValue = default)
        {
            return Caster.Extract<T>(Data, fieldNameOrPath, defaultValue);
        }

        /// <summary>
        /// Updates a single Record field value.
        /// </summary>
        public void Set(string fieldName, object value)
        {
            _data[fieldName] = JToken.FromObject(value);
        }

        public List<T> GetListValue<T>(string fieldNameOrPath, List<T> defaultValue = null)
        {
            return Get<List<T>>(fieldNameOrPath, defaultValue);
        }

        public string GetStringValue(string fieldNameOrPath, string defaultValue = null)
        {
            return Get<string>(fieldNameOrPath, defaultValue);
        }

        public bool GetBoolValue(string fieldNameOrPath, bool defaultValue = default)
        {
            return Get<bool>(fieldNameOrPath, defaultValue);
        }

        public int GetIntValue(string fieldNameOrPath, int defaultValue = default)
        {
            return Get<int>(fieldNameOrPath, defaultValue);
        }

        public float GetFloatValue(string fieldNameOrPath, float defaultValue = default)
        {
            return Get<float>(fieldNameOrPath, defaultValue);
        }

        public JToken this[string key]
        {
            get => _data.TryGetValue(key, out var value) ? value : null;
            set => _data[key] = value;
        }

        public override string ToString() => JsonConvert.SerializeObject(this);
    }
}