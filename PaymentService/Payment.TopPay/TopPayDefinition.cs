using PaymentCore;

namespace Payment.TopPay
{
    public class TopPayDefinition
    {
        public const string CallbackResponse = "SUCCESS";

        internal class Path
        {
            /// <summary>
            /// 創建代收
            /// </summary>
            public const string CreateOrder = "/gateway/prepaidOrder";

            /// <summary>
            /// 創建代付
            /// </summary>
            public const string CreatePayout = "/gateway/cash";
        }
        
        private static Dictionary<string, TransStatus> TopPayInStatusMap =>
            new()
            {
                { "SUCCESS", TransStatus.Success },
                { "INIT_ORDER", TransStatus.Pending },
                { "PAY_CANCEL", TransStatus.Fail },
                { "PAY_ERROR", TransStatus.Fail },
                { "NO_PAY", TransStatus.Fail }
                
            };
    
        private static Dictionary<string, TransStatus> TopPayoutStatusMap =>
            new()
            {
                { "2", TransStatus.Success },
                { "1", TransStatus.Pending },
                { "0", TransStatus.Pending },
                { "5", TransStatus.Pending},
                { "4", TransStatus.Fail }
            };
        
        public static TransStatus GetPayInTransStatus(string status)
            => TopPayInStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
    
        public static TransStatus GetPayoutTransStatus(string status)
            => TopPayoutStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
    }

    internal class MediaType
    {
        public const string Json = "application/json";
    }
}
