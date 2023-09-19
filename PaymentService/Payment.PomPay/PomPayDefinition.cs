using PaymentCore;

namespace Payment.PomPay;

public class PomPayDefinition
{
    public const string CallbackResponse = "ok";
    
    internal class Path
    {
        /// <summary>
        /// 創建代收
        /// </summary>
        public const string CreateOrder = "/payment";

        /// <summary>
        /// 創建代付
        /// </summary>
        public const string CreatePayout = "/v2/payout";
    }
    
    private static Dictionary<string, TransStatus> TransStatusMap =>
        new ()
        {
            { PayoutStatus.Success, TransStatus.Success },
            { PayoutStatus.New, TransStatus.Pending },
            { PayoutStatus.Processing, TransStatus.Pending },
            { PayoutStatus.Rejected, TransStatus.Fail },
            { PayoutStatus.Cancelled, TransStatus.Fail },
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
        public const string New = "new";
        
        public const string Processing = "processing";

        public const string Success = "success";

        public const string Rejected = "rejected";

        public const string Cancelled = "cancelled";
    }
}


internal class MediaType
{
    public const string Json = "application/json";
}