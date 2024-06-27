using Newtonsoft.Json.Linq;
using PaymentCore.Models;

namespace PaymentCore.Extensions
{
    public static class CommonExtension
    {
        public static string AddOrSetExchangeInfo(this string json, string strExchangeRate, string strExchangeAmount)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            var obj = JObject.Parse(json);
            obj.AddOrSet("ExchangeRate", strExchangeRate);
            obj.AddOrSet("ExchangeAmount", strExchangeAmount);

            return obj.ToString();
        }

        public static string AddOrSetJProperty(this string json, string propName, object propValue)
        {
            if (string.IsNullOrEmpty(json))
            {
                return json;
            }

            var obj = JObject.Parse(json);
            obj.AddOrSet(propName, JToken.FromObject(propValue));

            return obj.ToString();
        }

        public static bool StartsWithIgnoreCase(this string str, string target)
            => str.StartsWith(target, StringComparison.OrdinalIgnoreCase);

        public static void AddRange<T, S>(this IDictionary<T, S> source, IDictionary<T, S> collection)
        {
            foreach (var (key, value) in collection)
            {
                source.Add(key, value);
            }
        }

        public static void AddRange<T>(this IEnumerable<T> collection, IEnumerable<T> items)
        {
            items?.Aggregate(collection, (current, obj) => current.Append(obj));
        }
        
        public static bool IsNotOnlinePayment(this WithdrawalType type)
            => type is (WithdrawalType.LocalBank
                or WithdrawalType.CryptoWallet
                or WithdrawalType.Settlement
                or WithdrawalType.Undefined);
        
        public static bool IsNotOnlinePayment(this DepositType type)
            => type is (DepositType.LocalBank
                or DepositType.CryptoWallet);
    }
}