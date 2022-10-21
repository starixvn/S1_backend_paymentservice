using Newtonsoft.Json.Linq;
using PaymentCore.Models;

namespace PaymentCore.Extensions
{
    public static class CommonExtension
    {
        private static IEnumerable<int> _canRewardPointSiteIds = new []
        {
            (int) SiteEnum.Nova88
        };
        private static IEnumerable<int> _canRewardPointCurrencyIds = new []
        {
            (int) CurrencyEnum.MYR,
            (int) CurrencyEnum.THB,
            (int) CurrencyEnum.IDR
        };


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

        public static bool CanRewardPoint(int siteId, int currencyId)
            => _canRewardPointSiteIds.Contains(siteId)
               && _canRewardPointCurrencyIds.Contains(currencyId);
    }
}