using PaymentCore;

namespace Payment.DsPay
{
    internal class Definition
    {
        public const string CallBackResponse = "SUCCESS";
        public const int SuccessCode = 0;

        internal class Path
        {
            public const string Deposit = "/api/mch/unifiedorder";
        }

        private static Dictionary<string, TransStatus> StatusMap =>
            new()
            {
                // 未出码
                { "0", TransStatus.Pending },
                // 待支付
                { "2", TransStatus.Pending },
                // 被风控
                { "5", TransStatus.Pending },
                // 交易失败
                { "1", TransStatus.Fail },
                // 已支付
                { "3", TransStatus.Success },
                // 冲正
                { "4", TransStatus.Success }
            };

        
        public static TransStatus GetTransStatus(string status)
        {
            return StatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }
    }
}