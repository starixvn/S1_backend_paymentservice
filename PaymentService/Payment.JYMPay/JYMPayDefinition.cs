using PaymentCore;

namespace Payment.JYMPay
{
    public class JYMPayDefinition
    {
        public const string CallbackSuccessResponse = "0000";

        public const string CallbackPendingResponse = "5555";
        
        public const string CallbackFailResponse = "9999";

        internal class Path
        {
            /// <summary>
            /// 創建代收
            /// </summary>
            public const string CreateOrder = "/v1/b2b/payment-orders/place-order";

            /// <summary>
            /// 創建代付
            /// </summary>
            public const string CreatePayout = "/v1/b2b/payout-orders/place-order";
        }
        
        private static Dictionary<string, TransStatus> PayoutTransStatusMap =>
            new()
            {
                { PayoutStatus.Completed, TransStatus.Success },
                { PayoutStatus.Failed, TransStatus.Fail },
                { PayoutStatus.Reject, TransStatus.Fail },
            };
        
        private static Dictionary<string, TransStatus> DebitTransStatusMap =>
            new()
            {
                { DebitStatus.Completed, TransStatus.Success },
                { DebitStatus.Failed, TransStatus.Fail },
                { DebitStatus.Reject, TransStatus.Fail },
            };



        public static TransStatus GetTransStatus(string status,int type)
        {
            //  type = 1 (Debit), type = 2 (payout)
            switch (type)
            {
                case 1:
                    if (DebitTransStatusMap.TryGetValue(status, out var debitResult))
                    {
                        return debitResult;
                    }
                    break;
                case 2:
                    if (PayoutTransStatusMap.TryGetValue(status, out var payoutResult))
                    {
                        return payoutResult;
                    }
                    break;
            }
            return TransStatus.Pending;
        }

        private class PayoutStatus
        {
            
            public const string Completed = "3";
            
            public const string Failed = "5";

            public const string Reject = "4";
        }

        private class DebitStatus
        {
            public const string Completed = "2";
            
            public const string Failed = "4";

            public const string Reject = "3";
        }
    }
    
    internal class MediaType
    {
        public const string Json = "application/json";
    }
}