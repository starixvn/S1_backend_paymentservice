using PaymentCore;

namespace Payment.YFBPay
{
    public class Definition
    {
        public const string SuccessCode = "0";
        public const string CallbackResponse = "success";

        private static Dictionary<string, TransStatus> DepositStatusMap =>
            new()
            {
                { "PAID", TransStatus.Success },
                { "MANUAL PAID", TransStatus.Success }
            };

        private static Dictionary<string, TransStatus> WithdrawalStatusMap =>
            new()
            {
                { "PAID", TransStatus.Success },
                { "CANCELLED", TransStatus.Fail }
            };

        private static Dictionary<string, string> CurrencyMap =>
            new()
            {
                { "4", "THB" },
                { "51", "VND" }
            };

        public static string GetPayCurrency(string currencyId)
            => CurrencyMap.GetValueOrDefault(currencyId, string.Empty);

        public static TransStatus GetDepositTransStatus(string status)
            => DepositStatusMap.GetValueOrDefault(status.ToUpper(), TransStatus.Pending);

        public static TransStatus GetWithdrawalTransStatus(string status)
            => WithdrawalStatusMap.GetValueOrDefault(status.ToUpper(), TransStatus.Pending);
    }

    internal class Path
    {
        public const string Deposit = "/order/create";
        public const string Withdrawal = "/payout/create";
    }
}