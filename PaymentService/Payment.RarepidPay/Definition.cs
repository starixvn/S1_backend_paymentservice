using PaymentCore;

namespace Payment.RarepidPay
{
    internal class Definition
    {
        internal class Path
        {
            /// <summary>
            /// 存款
            /// </summary>
            public const string Deposit = "/v2/Deposit";
            /// <summary>
            /// 提款
            /// </summary>
            public const string Withdrawal = "/v2/Withdraw";
        }

        public const int SuccessResponseCode = 0;
        public const string CallBackResponse = "Ok";

        private static Dictionary<string, TransStatus> StatusMap =>
            new()
            {
                { CallBackStatus.Initiated, TransStatus.Pending },
                { CallBackStatus.Pending, TransStatus.Pending},
                { CallBackStatus.Closed, TransStatus.Pending },
                { CallBackStatus.Completed, TransStatus.Success }
            };


        private class CallBackStatus
        {
            public const string Initiated = "initiated";
            public const string Pending = "pending";
            public const string Closed = "closed";
            public const string Completed = "completed";
        }

        public static TransStatus GetTransStatus(string status)
        {
            return StatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }
    }
}