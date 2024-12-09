using PaymentCore;

namespace Payment.AeePay
{
    internal class Definition
    {
        internal class Path
        {
            /// <summary>
            /// 存款
            /// </summary>
            public const string Deposit = "/api/payment";
            /// <summary>
            /// 提款
            /// </summary>
            public const string Withdrawal = "/api/withdraw";
        }

        private static Dictionary<string, TransStatus> DepositCallBackStatusMap =>
            new()
            {
                { DepositCallBackStatus.Fail, TransStatus.Fail },
                { DepositCallBackStatus.Success, TransStatus.Success }
            };
        
        private class DepositCallBackStatus
        {
            public const string Success = "1";
            public const string Fail = "2";
        }

        public static TransStatus GetDepositTransStatus(string status)
        {
            return DepositCallBackStatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }

        private static Dictionary<string, TransStatus> WithdrawalCallBackStatusMap =>
            new()
            {
                { WithdrawalCallBackStatus.Fail, TransStatus.Fail },
                { WithdrawalCallBackStatus.Success, TransStatus.Success }
            };

        private class WithdrawalCallBackStatus
        {
            public const string Success = "3";
            public const string Fail = "4";
        }

        public static TransStatus GetWithdrawalTransStatus(string status)
        {
            return WithdrawalCallBackStatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }
    }
    
    internal class CallbackResponse
    {
        public const string Success = "ok";
    }
}
