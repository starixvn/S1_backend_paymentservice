using Newtonsoft.Json.Linq;

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
    }
}