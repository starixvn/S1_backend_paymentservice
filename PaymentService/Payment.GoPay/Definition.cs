using PaymentCore;

namespace Payment.GoPay;

public class Definition
{
    public const string CallbackResponse = "success";

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
    public const string CreateOrder = "/trade/create";

    public const string CreatePayout = "/trade/payout/create";
}