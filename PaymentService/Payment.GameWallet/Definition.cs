using PaymentCore;

namespace Payment.GameWallet
{
    internal class Definition
    {
        internal const string CallBackResponse = "";
        internal const string WithDrawalSuccessCode = "002";

        private static Dictionary<string, TransStatus> DebitStatusMap =>
            new()
            {
                { "006", TransStatus.Success },
                { "001", TransStatus.Fail },
                { "007",  TransStatus.Fail},
                { "009",  TransStatus.Pending},
            };

        private static Dictionary<string, TransStatus> CreditStatusMap =>
        new()
        {
                { "000", TransStatus.Success },
                { "001", TransStatus.Fail },
                {"002", TransStatus.Pending }
        };

        private static Dictionary<string, string> CurrencyMap =>
            new()
            {
                { "4", "THB" }
            };

        internal static TransStatus GetCreditTransStatus(string status)
            => CreditStatusMap.GetValueOrDefault(status, TransStatus.Pending);

        internal static TransStatus GetTransStatus(string status)
            => DebitStatusMap.GetValueOrDefault(status, TransStatus.Pending);

        public static string GetPayCurrency(string currencyId)
            => CurrencyMap.GetValueOrDefault(currencyId, string.Empty);
    }
}