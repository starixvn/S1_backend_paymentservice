using PaymentCore;

namespace Payment.CashRuPay
{
    internal class Definition
    {
        public const string CallbackResponse = "success";

        public const string SuccessCode = "0";

        private static Dictionary<string, TransStatus> TransStatusMap =>
            new()
            {
                { "1", TransStatus.Success },
                { "0", TransStatus.Fail }
            };

        public static TransStatus GetTransStatus(string status)
            => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    }

    internal class Path
    {
        public const string CreateDeposit = "/web/createOnlineOrder";
        public const string CreateWithdrawal = "/web/createOnlineOutOrder";
    }
}