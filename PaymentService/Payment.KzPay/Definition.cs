using PaymentCore;

namespace Payment.KzPay;

public class Definition
{
    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { "0", TransStatus.Pending },
            { "1", TransStatus.Pending },
            { "2", TransStatus.Fail },
            { "3", TransStatus.Success },
            { "4", TransStatus.Fail },
            { "5", TransStatus.Fail }
        };
    public static TransStatus GetTransStatus(string status)
        => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    public const string SuccessCode = "0";
}


internal class Path
{
    /// <summary>
    /// 創建代收
    /// </summary>
    public const string CreatePay = "/api/pay";

    /// <summary>
    /// 創建代付
    /// </summary>
    public const string CreatePayout = "/api/transfer";
}


internal class CallbackResponse
{
    public const string Success = "success";

    public const string Failed = "failed";
}