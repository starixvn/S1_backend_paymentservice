using PaymentCore;

namespace Payment.BCOTC
{
    internal class Definition
    {
        internal class Path
        {
            /// <summary>
            /// 存款
            /// </summary>
            public const string Deposit = "/api/v2/merchant-orders";
            /// <summary>
            /// 提款
            /// </summary>
            public const string Withdrawal = "/api/v2/merchant-payout-requests";
        }

        private static Dictionary<string, TransStatus> DepositStatusMap =>
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "unpaid", TransStatus.Pending },
                { "paid", TransStatus.Pending },
                { "completed", TransStatus.Success },
                { "canceled", TransStatus.Fail },
                { "appealed", TransStatus.Pending }
            };

        public static TransStatus GetDepositTransStatus(string status)
        {
            return DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }

        private static Dictionary<string, TransStatus> WithdrawalStatusMap =>
            new(StringComparer.OrdinalIgnoreCase)
            {
                { "pending", TransStatus.Pending },
                { "processing", TransStatus.Pending },
                { "paid", TransStatus.Pending },
                { "canceled", TransStatus.Fail },
                { "completed", TransStatus.Success }
            };

        public static TransStatus GetWithdrawalTransStatus(string status)
        {
            return WithdrawalStatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }
    }

    internal class CallbackResponse
    {
        public const string Success = "OK";
    }
}
