using PaymentCore;

namespace Payment.XcPay
{
    internal class Definition
    {
        public const string QR = "IndiaQR";
        public const string Payout = "IndiaPayout";
        internal class CryptoMethod
        {
            /// <summary>
            /// MD5
            /// </summary>
            public const string MD5 = "MD5";
        }

        internal class Path
        {
            /// <summary>
            /// 存款
            /// </summary>
            public const string deposit = "/api/gateway/pay";
            /// <summary>
            /// 提款
            /// </summary>
            public const string Withdrawal = "/api/gateway/withdraw";
        }

        private static Dictionary<string, TransStatus> DepositStatusMap =>
            new()
            {
                { DepositCallBackStatus.Unpaid, TransStatus.Fail },
                { DepositCallBackStatus.Paid, TransStatus.Success },
                { DepositCallBackStatus.Faild, TransStatus.Pending},
            };
        private static Dictionary<string, TransStatus> WithdrawalStatusMap =>
            new()
            {
                { WithdrawalCallBackStatus.Success, TransStatus.Success },
                { WithdrawalCallBackStatus.Pending, TransStatus.Pending },
                { WithdrawalCallBackStatus.Reject, TransStatus.Fail},
            };

        private class DepositCallBackStatus
        {
            public const string Unpaid = "0";
            public const string Paid = "1";
            public const string Faild = "4";
        }

        private class WithdrawalCallBackStatus
        {
            public const string Success = "1";
            public const string Pending = "2";
            public const string Reject = "5";
        }

        public static TransStatus GetTransStatus(string status, string method)
        {
            if (method == QR) return DepositStatusMap.GetValueOrDefault(status, TransStatus.Pending);
            if (method == Payout) return WithdrawalStatusMap.GetValueOrDefault(status, TransStatus.Pending);
            return TransStatus.Pending;
        }

    }
}
