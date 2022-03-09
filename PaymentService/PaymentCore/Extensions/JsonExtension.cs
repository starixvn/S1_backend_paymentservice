using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;

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
    }
}
