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
            public const string Withdrawal = "/ajax/api/withdraw";
        }

        public const int SuccessResponseCode = 0;
    }
}