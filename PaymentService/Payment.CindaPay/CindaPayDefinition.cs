using PaymentCore;

namespace Payment.CindaPay;

public class CindaPayDefinition
{
    public const string SuccessCode = "0";

    public class Version
    {
        public const string Deposit = "1.6";

        public const string Payout = "1.0";
    }

    public class Path
    {
        public const string Deposit = "/louis/gateway.do";

        public const string Payout = "/louis/ap.do";
    }

    public const string CallbackResponse = "SUCCESS";

    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { CindaPayStatus.Success, TransStatus.Success },
            { CindaPayStatus.Processing, TransStatus.Pending },
            { CindaPayStatus.Failed, TransStatus.Fail },
            { CindaPayStatus.Cancel, TransStatus.Fail }
        };


    public static TransStatus GetTransStatus(string status)
        => TransStatusMap.TryGetValue(status, out var result)
            ? result : TransStatus.Pending;

    private class CindaPayStatus
    {
        public const string Success = "1";

        public const string Processing = "2";

        public const string Failed = "3";

        public const string Cancel = "4";
    }
}