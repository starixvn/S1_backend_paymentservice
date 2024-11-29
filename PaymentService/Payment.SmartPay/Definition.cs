using PaymentCore;

namespace Payment.SmartPay
{
    internal class Definition
    {
        internal const string CallbackResponse = "SUCCESS";
        internal const string SuccessCode = "0";

        private static Dictionary<string, TransStatus> StatusMap =>
            new()
            {
                { "1", TransStatus.Success },
                { "2", TransStatus.Fail }
            };

        internal static TransStatus GetTransStatus(string status)
            => StatusMap.GetValueOrDefault(status, TransStatus.Pending);

        internal class Path
        {
            public const string Deposit = "/api/pay/V2";
            public const string Withdrawal = "/api/defray/V2";
        }
    }
}