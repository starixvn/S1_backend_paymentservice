using PaymentCore;

namespace Payment.TopPayWorld
{
    public class TopPayWorldDefinition
    {
        public const string CallbackResponse = "SUCCESS";

        internal class Path
        {
            /// <summary>
            /// 創建代收
            /// </summary>
            public const string CreateOrder = "/gateway/online/transaction";

            /// <summary>
            /// 創建代付
            /// </summary>
            public const string CreatePayout = "/gateway/online/transaction";
        }
        
        private static Dictionary<string, TransStatus> TopPayWorldStatusMap =>
            new()
            {
                { "PAYMENT_DONE", TransStatus.Success },
                { "PAYMENT_PROGRESS", TransStatus.Pending },
                { "BECONFIRMED", TransStatus.Pending },
                { "ABNORMAL", TransStatus.Pending },
                { "PAYMENT_CLOSE", TransStatus.Fail },
                { "FAILED", TransStatus.Fail },
                { "REJECT", TransStatus.Fail }
                
            };
        
        public static TransStatus GetPayInTransStatus(string status)
            => TopPayWorldStatusMap.GetValueOrDefault(status, TransStatus.Pending);
        
    }

    internal class MediaType
    {
        public const string Json = "application/json";
    }
    
    internal class AcceptTimezone
    {
        public const string Timezone = "UTC";
    }
}
