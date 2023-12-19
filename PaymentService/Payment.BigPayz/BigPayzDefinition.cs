using PaymentCore;

namespace Payment.BigPayz;

internal class BigPayzDefinition
{
    internal class Path
    {
        public const string Deposit = "/payin/depositv2";

        public const string QRDeposit = "/QRScanDeposit";

        public const string Withdrawal = "/payout/withdrawal";
    }

    internal class ErrorCode
    {
        public const string Success = "0";
    }

    public const string CallbackResponse = "SUCCESS";

    public static Dictionary<string, TransStatus> BigPayzStatusMap =>
        new()
        {
            { BigPayzStatus.Approved, TransStatus.Success },
            { BigPayzStatus.Success, TransStatus.Success },
            { BigPayzStatus.Rejected, TransStatus.Fail },
            { BigPayzStatus.Pending, TransStatus.Pending }
        };

    public static TransStatus GetTransStatus(string status)
        => BigPayzStatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;

    internal class BigPayzStatus
    {
        public const string Pending = "0";
        public const string Success = "1";
        public const string Rejected = "2";
        public const string Approved = "3";
    }
}
