using PaymentCore;

namespace Payment.MbmPay
{
    internal class Definition
    {        
        internal const string CallbackResponse = "success";
        internal const string SuccessCode = "0";

        private static Dictionary<string, TransStatus> DebitStatusMap =>
            new()
            {
                { "PAID", TransStatus.Success }
            };
        
        private static Dictionary<string, TransStatus> CreditStatusMap =>
            new()
            {
                { "SUCCESS", TransStatus.Success },
                { "FAILED", TransStatus.Fail },
                { "CANCEL", TransStatus.Fail }
            };

        internal static TransStatus GetDebitTransStatus(string status)
            => DebitStatusMap.GetValueOrDefault(status.ToUpper(), TransStatus.Pending);

        internal static TransStatus GetCreditTransStatus(string status)
            => CreditStatusMap.GetValueOrDefault(status.ToUpper(), TransStatus.Pending);

        internal class Path
        {
            public const string Deposit = "/api/deposit/order";
            public const string Withdrawal = "/api/payout/order";
        }
    }
}
