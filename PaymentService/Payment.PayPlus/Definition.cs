using PaymentCore;

namespace Payment.PayPlus;

public class Definition
{
    private static Dictionary<string, TransStatus> PayinStatusMap =>
        new()
        {
            { "PENDING", TransStatus.Pending },
            { "SUCCESS", TransStatus.Success },
            { "FAIL", TransStatus.Fail },
            { "CANCEL", TransStatus.Fail }
        };
    private static Dictionary<string, TransStatus> PayoutStatusMap =>
        new()
        {
            { "PAYMENT", TransStatus.Pending },
            { "SUCCESS", TransStatus.Success },
            { "REFUNDED", TransStatus.Fail },
            { "UNREFUND", TransStatus.Pending },
            { "DEBIT", TransStatus.Pending }
        };
    
    public static TransStatus GetPayinTransStatus(string status)
        => PayinStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutStatusMap.GetValueOrDefault(status, TransStatus.Pending);
}


internal class CallbackResponse
{
    public const string Success = "success";
    public const string Failed = "failed";
}