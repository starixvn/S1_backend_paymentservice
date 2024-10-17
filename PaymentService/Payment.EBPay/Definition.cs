using PaymentCore;

namespace Payment.EBPay;

public class Definition
{
    public const string SuccessCode = "200";
    
    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { "1", TransStatus.Success },
            { "2", TransStatus.Fail }
        };


    public static TransStatus GetTransStatus(string status)
        => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
}

internal class Path
{
    public const string CreateOrder = "/api/merchant/deposit/v2";

    public const string CreatePayout = "/api/merchant/withdraw/create/v2";
}