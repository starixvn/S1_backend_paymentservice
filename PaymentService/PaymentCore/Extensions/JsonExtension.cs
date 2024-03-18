using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace PaymentCore.Extensions
{
    public static class JsonExtension
    {
        public static bool IsJson(this string str)
        {
            if (string.IsNullOrEmpty(str))
                return true;

            var value = str.Trim();

            return (value.StartsWith("{") && value.EndsWith("}"))
                   || (value.StartsWith("[") && value.EndsWith("]"));
        }

        public static JObject AddOrSet(this JObject obj, string key, JToken value)
        {
            if (obj.ContainsKey(key))
            {
                obj[key] = value;
            }
            else
            {
                obj.Add(key, value);
            }

            return obj;
        }

        public static T DeserializeObject<T>(string str)
            => JsonConvert.DeserializeObject<T>(str, new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            });
    }
}
