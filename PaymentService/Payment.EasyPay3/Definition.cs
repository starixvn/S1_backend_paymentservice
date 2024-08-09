using PaymentCore;

namespace Payment.EasyPay3;

public class Definition
{
    public const string CallbackResponse = "ok";

    internal class Path
    {
        /// <summary>
        /// 創建代收
        /// </summary>
        public const string CreateOrder = "/api/MM/RegCharge";

        /// <summary>
        /// 創建代付
        /// </summary>
        public const string CreatePayout = "/api/MM/ChargeOut";
    }

    public static Dictionary<int, string> DepositErrorMsgMap =>
    new()
    {
            { 1, "request success" },
            { 0, "request fail (Try again in a few minutes)" },
            { -1, "The server is busy, maintenance" },
            { -2, "Incorrect connection information, invalid apikey, account locked.." }
    };

    public class ErrorCode
    {
        public const int Success = 1;
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
        => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);


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