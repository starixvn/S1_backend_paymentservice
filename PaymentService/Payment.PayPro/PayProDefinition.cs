using PaymentCore;

namespace Payment.PayPro
{
    public class PayProDefinition
    {
        
        public const string CallbackResponse = "success";
        
        public const string CallbackFailResponse = "fail";

        internal class Path
        {
            /// <summary>
            /// 創建代收
            /// </summary>
            public const string CreateOrder = "/api/merchant/transact";

            /// <summary>
            /// 創建代付
            /// </summary>
            public const string CreatePayout = "/api/merchant/transact";
        }
        
        private static Dictionary<int, TransStatus> PayProInStatusMap =>
            new()
            {
                { 1, TransStatus.Success },
                { 4, TransStatus.Pending },
                { 2, TransStatus.Fail },
                { 5, TransStatus.Fail },
                { 6, TransStatus.Fail },
                { 7, TransStatus.Fail }
                
            };
    
        private static Dictionary<int, TransStatus> PayProOutStatusMap =>
            new()
            {
                { 1, TransStatus.Success },
                { 2, TransStatus.Fail },
                { 4, TransStatus.Fail }
            };
        
        public static TransStatus GetPayInTransStatus(int status)
            => PayProInStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
    
        public static TransStatus GetPayOutTransStatus(int status)
            => PayProOutStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
    
    }
    
    internal class MediaType
    {
        public const string Json = "application/json";
    }
    
}
