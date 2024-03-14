using PaymentCore;

namespace Payment.QuickPay
{
    public class QuickPayDefinition
    {
        public const string CallbackResponse = "success";

        internal class Path
        {
            /// <summary>
            /// 創建代收
            /// </summary>
            public const string CreateOrder = "order/create";

            /// <summary>
            /// 創建代付
            /// </summary>
            public const string CreatePayout = "order/payout_create";
        }

        private static Dictionary<string, TransStatus> TransStatusMap =>
            new()
            {
                { PayoutStatus.Completed, TransStatus.Success },
                { PayoutStatus.Canceled, TransStatus.Fail }
            };


        public static TransStatus GetTransStatus(string status)
        {
            if (TransStatusMap.TryGetValue(status, out var result))
            {
                return result;
            }

            return TransStatus.Pending;
        }

        private class PayoutStatus
        {

            public const string Completed = "PAID";
            public const string Canceled = "CANCELLED";
        }
    }


    internal class MediaType
    {
        public const string Json = "application/json";
    }
}