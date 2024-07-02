using PaymentCore;

namespace Payment.HeroPay;

public class Definition
{
    private static Dictionary<string, TransStatus> DepositTransStatusMap =>
        new()
        {
            { "0", TransStatus.Pending },
            { "1", TransStatus.Pending },
            { "3", TransStatus.Success },
            { "5", TransStatus.Fail }
        };
    
    private static Dictionary<string, TransStatus> PayoutTransStatusMap =>
        new()
        {
            { "0", TransStatus.Pending },
            { "1", TransStatus.Pending },
            { "3", TransStatus.Fail },
            { "2", TransStatus.Success }
        };
    
    
    public static TransStatus GetDepositTransStatus(string status)
        => DepositTransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutTransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    public const string SuccessCode = "SUCCESS";
}


internal class Path
{
    /// <summary>
    /// 創建代收
    /// </summary>
    public const string CreatePay = "/api/pay/create_order";

    /// <summary>
    /// 創建代付
    /// </summary>
    public const string CreatePayout = "/api/trans/create_order";
}


internal class CallbackResponse
{
    public const string Success = "success";

    public const string Failed = "failed";
}