using PaymentCore;

namespace Payment.DgPay
{
    internal class Definition
    {
        internal class Path
        {
            /// <summary>
            /// 存款
            /// </summary>
            public const string Deposit = "/ajax/api/deposit";
            /// <summary>
            /// 提款
            /// </summary>
            public const string Withdrawal = "/ajax/api/withdraw";
        }

        private static Dictionary<string, TransStatus> StatusMap =>
            new()
            {
                { CallBackStatus.Expired, TransStatus.Fail },
                { CallBackStatus.Reject, TransStatus.Fail },
                { CallBackStatus.Pending, TransStatus.Pending},
                { CallBackStatus.Processing, TransStatus.Pending },
                { CallBackStatus.Success, TransStatus.Success }
            };


        private class CallBackStatus
        {
            public const string Expired = "-20";
            public const string Reject = "-10";
            public const string Pending = "0";
            public const string Processing = "10";
            public const string Success = "20";
        }

        public static TransStatus GetTransStatus(string status)
        {
            return StatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }
    }
}
