using PaymentCore;

namespace Payment.AOPay;

public class Definition
{
    public const string CallbackResponse = "ok";

    internal class Path
    {
        /// <summary>
        /// 創建代收
        /// </summary>
        public const string CreateOrder = "/api/transaction";

        /// <summary>
        /// 創建代付
        /// </summary>
        public const string CreatePayout = "/api/payment";
    }

    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { PayoutStatus.Completed, TransStatus.Success },
            { PayoutStatus.New, TransStatus.Pending },
            { PayoutStatus.Processing, TransStatus.Pending },
            { PayoutStatus.Refund, TransStatus.Fail },
            { PayoutStatus.Failed, TransStatus.Fail },
            { PayoutStatus.Reject, TransStatus.Fail }
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

        public const string Completed = "completed";

        public const string Refund = "refund";

        public const string Failed = "failed";

        public const string Reject = "reject";
    }
}


internal class MediaType
{
    public const string Json = "application/json";
}