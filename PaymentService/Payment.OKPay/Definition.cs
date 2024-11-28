using PaymentCore;

namespace Payment.OKPay;

internal class Definition
{
    public const string CallbackResponse = "SUCCESS";
    public const string SuccessCode = "200";

    private static Dictionary<string, TransStatus> TransStatusMap =>
        new()
        {
            { "SUCCESS", TransStatus.Success },
            { "FAIL", TransStatus.Fail }
        };
    
    public static TransStatus GetTransStatus(string status)
        => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);
}


internal class Path
{
    public const string CreateOrder = "/api/v1/order/pay";

    public const string CreatePayout = "/api/v1/order/behalfPay";
}