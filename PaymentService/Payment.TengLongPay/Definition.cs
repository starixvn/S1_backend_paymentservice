using PaymentCore;

namespace Payment.TengLongPay;

internal class Definition
{
    private static Dictionary<string, TransStatus> PayoutStatusMap =>
        new()
        {
            { "3", TransStatus.Success },
            { "4", TransStatus.Fail },
            { "5", TransStatus.Fail }
        };
    
    public static TransStatus GetPayoutStatus(string status)
        => PayoutStatusMap.GetValueOrDefault(status, TransStatus.Pending);
}

internal static class CallbackResponse
{
    public const string Success = "success";
    public const string Failed = "failed";
}

internal class Path
{
    public const string CreateOrder = "/api/index/orderdoing";
    
    public const string CreatePayout = "/v1/dfapi/add";
}