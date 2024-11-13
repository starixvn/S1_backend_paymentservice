using PaymentCore;

namespace Payment.LuckyPay;

public class Definition
{
    public const string CallbackResponse = "ok";

    public const int SuccessCode = 200;
    

    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { "PAID", TransStatus.Success },
            { "FINISH", TransStatus.Success },
            { "CANCEL", TransStatus.Fail },
            { "FAILED", TransStatus.Fail },
            { "REVERT", TransStatus.Fail }
        };


    public static TransStatus GetTransStatus(string status)
        => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
}

internal class Path
{
    public const string CreateOrder = "/api/coin/pay/request";

    public const string CreatePayout = "/api/bank/agentPay/request";
}