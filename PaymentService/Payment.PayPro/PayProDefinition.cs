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
    
    }
    
    internal class MediaType
    {
        public const string Json = "application/json";
    }
    
}
