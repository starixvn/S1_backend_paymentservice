using PaymentCore;

namespace Payment.MTPay
{
    public class MTPayDefinition
    {
        public const string CallbackResponse = "SUCCESS";

        internal class Path
        {
            /// <summary>
            /// 創建代收
            /// </summary>
            public const string CreateOrder = "/Index";

            /// <summary>
            /// 創建代付
            /// </summary>
            public const string CreatePayout = "/Payment";
        }
    }
    
    internal class MediaType
    {
        public const string Json = "application/json";
    }
}