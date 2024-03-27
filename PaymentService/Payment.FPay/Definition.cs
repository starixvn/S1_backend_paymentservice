using PaymentCore;

namespace Payment.FPay;

internal static class Definition
{
    private static Dictionary<string, TransStatus> StatusMap =>
        new()
        {
            { "completed", TransStatus.Success },
            { "fail", TransStatus.Fail }
        };
    
    public static TransStatus GetTransStatus(string status)
        => StatusMap.GetValueOrDefault(status, TransStatus.Pending);
}

internal static class CallbackResponse
{
    public const string Success = "200";
    public const string Failed = "failed";
}

internal class Path
{
    public const string Auth = "/merchant/auth";

    
    /// <summary>
    /// 創建代收
    /// </summary>
    public const string CreateOrder = "/merchant/generate_orders";

    /// <summary>
    /// 創建代付
    /// </summary>
    public const string CreatePayout = "/merchant/withdraw_orders";
}