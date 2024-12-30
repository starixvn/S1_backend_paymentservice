using PaymentCore;

namespace Payment.MultiPay
{
    public class Definition
    {
        internal const string CallbackResponse = "success";
        internal const int SuccessCode = 200;
        internal const int FailDataStatus = 2;

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
            public const string Deposit = "/api/payin/order";
            public const string Withdrawal = "/api/payout/order";
        }
    }
}