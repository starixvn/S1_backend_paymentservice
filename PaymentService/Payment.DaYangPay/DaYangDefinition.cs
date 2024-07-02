namespace Payment.DaYangPay
{
    public class DaYangDefinition
    {
        public const string CallbackResponse = "SUCCESS";

        internal class Path
        {
            /// <summary>
            /// 創建代收
            /// </summary>
            public const string CreateOrder = "/api/v1/trades";

            /// <summary>
            /// 創建代付
            /// </summary>
            public const string CreatePayout = "/api/v1/transfers";
        }   

    }
    
    internal class MediaType
    {
        public const string Json = "application/json";
    }
}