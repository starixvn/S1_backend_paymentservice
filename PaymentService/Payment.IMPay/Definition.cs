using PaymentCore;

namespace Payment.IMPay;

internal class Definition
{
    private static Dictionary<string, TransStatus> StatusMap =>
        new()
        {
            { "SUCCESS", TransStatus.Success },
            { "CANCELLED", TransStatus.Fail },
            { "REJECTED", TransStatus.Fail }
        };
    
    public static TransStatus GetTransStatus(string status)
        => StatusMap.GetValueOrDefault(status.ToLower(), TransStatus.Pending);
    
    public const string SuccessStatus = "success";
    public const string CallbackResponse = "success";
}

internal class Path
{
    public const string CreateDeposit = "/api/impay/deposit";
    public const string CreatePayout = "/api/impay/withdrawal";
}