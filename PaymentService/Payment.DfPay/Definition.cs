using PaymentCore;

namespace Payment.DfPay
{
    internal class Definition
    {
        internal class Path
        {
            /// <summary>
            /// 提款
            /// </summary>
            public const string Withdrawal = "/app/DFPayFun";
        }

        private static Dictionary<string, TransStatus> StatusMap =>
            new()
            {
                { CallBackStatus.Processing, TransStatus.Pending },
                { CallBackStatus.Success, TransStatus.Success },
                { CallBackStatus.Fail, TransStatus.Fail }
            };


        private class CallBackStatus
        {
            // 0处理中
            public const string Processing = "0";

            // 1已处理
            public const string Success = "1";

            // 2失败已退回
            public const string Fail = "2";
        }

        public static TransStatus GetTransStatus(string status)
        {
            return StatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }
    }

    internal class CallbackResponse
    {
        public const string Success = "SUCCESS";
    }
}