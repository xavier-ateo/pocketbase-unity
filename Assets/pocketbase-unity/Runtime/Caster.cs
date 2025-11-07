using Newtonsoft.Json.Linq;

namespace PocketBaseSdk
{
    public static class Caster
    {
        /// <summary>
        /// <para>
        /// Extracts a single value from <paramref name="data"/> by a dot-notation path
        /// and tries to cast it to the specified generic type.
        /// </para>
        /// <para>
        /// If explicitly set, returns <paramref name="defaultValue"/> on missing path.
        /// </para>
        /// </summary>
        public static T Extract<T>(JObject data, string fieldNameOrPath, T defaultValue = default)
        {
            string[] pathParts = fieldNameOrPath.Trim().Split('.');
            JToken value = data;

            try
            {
                foreach (var pathPart in pathParts)
                {
                    if (value == null)
                    {
                        return defaultValue;
                    }
                    value = value[pathPart];
                }

                // Check if value is null before calling ToObject
                if (value == null)
                {
                    return defaultValue;
                }

                return value.ToObject<T>();
            }
            catch (System.Exception)
            {
                return defaultValue;
            }
        }
    }
}