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

    public static Dictionary<int, string> StatusMsgMap =>
    new()
    {
            { 1, "request success" },
            { 0, "request fail (Try again in a few minutes)" },
            { -1, "The server is busy, maintenance" },
            { -2, "Incorrect connection information, invalid apikey, account locked.." }
    };

    public static Dictionary<double, string> ExtraStatusMsgMap =>
   new()
   {
            { 1.0, "success" },
            { 1.1, "duplicate unique_request" },
            { 0.0, "wrong signature" },
            { 0.1, "withdrawal amount has not been set to a minimum" },
            { 0.2, "Receiving bank is under maintenance" },
            { 0.3, "wrong bank_code information" },
            { 0.4, "missing beneficiary account owner information" },
            { 0.51, "Transaction revalidation failed" },
            { 0.52, "incorrect transaction revalidation signature" },
            { -1.0, "server is busy, maintenance" },
            { -2.0, "wrong apikey information, or whilelist ip" },
            { -2.1, "locked api" },
            { -2.2, "sending request too fast" },
            { -2.3, "you don't have enough balance" }
   };

    public static Dictionary<string, string> WithdrawalDataStatusMap =>
new()
{
            { "waiting", "transactions are placed in the queue and waiting for execution" },
            { "pending", "the transaction is temporarily held, needs to be confirmed again before it can be carried out" }
};

    public class ErrorCode
    {
        public const int Success = 1;
    }

    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { PayInAndPayoutStatus.Success, TransStatus.Success },
            { PayInAndPayoutStatus.Timeout, TransStatus.Fail },
            { PayInAndPayoutStatus.Unknown, TransStatus.Success },
            { PayInAndPayoutStatus.Deleted, TransStatus.Fail },
            { PayInAndPayoutStatus.NCheck, TransStatus.Pending }
        };


    public static TransStatus GetTransStatus(string status)
        => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);


    private class PayInAndPayoutStatus
    {
        public const string Success = "success";

        public const string Timeout = "timeout";

        public const string Unknown = "unknown";

        public const string Deleted = "deleted";

        public const string NCheck = "nCheck";
    }
}


internal class MediaType
{
    public const string Json = "application/json";
}