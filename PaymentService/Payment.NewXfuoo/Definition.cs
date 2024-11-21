using PaymentCore;

namespace Payment.NewXfuoo
{
    internal class Definition
    {
        internal class Path
        {
            /// <summary>
            /// 代收
            /// </summary>
            public const string Deposit = "/usdt/payin";

            /// <summary>
            /// 代付
            /// </summary>
            public const string Withdrawal = "/usdt/payout";

            public const string GetExchangeRate = "/usdt/rate";
        }

        private static Dictionary<string, TransStatus> DepositStatusMap =>
            new()
            {
                // 处理中
                { "0", TransStatus.Pending },
                // 成功
                { "1", TransStatus.Success },
                // 失败
                { "2", TransStatus.Fail },
                // 冻结
                { "3", TransStatus.Fail }
            };
        private static Dictionary<string, TransStatus> WithdrawalStatusMap =>
            new()
            {
                // 处理中
                { "0", TransStatus.Pending },
                // 成功
                { "1", TransStatus.Success },
                // 失败
                { "2", TransStatus.Fail },
                // 挂起
                { "3", TransStatus.Pending },
                // 审核中
                { "4", TransStatus.Pending },
                // 待提交
                { "5", TransStatus.Pending }
            };

        public static TransStatus GetDepositTransStatus(string status)
        {
            return DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }
        public static TransStatus GetWithdrawalTransStatus(string status)
        {
            return WithdrawalStatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }
    }
}
