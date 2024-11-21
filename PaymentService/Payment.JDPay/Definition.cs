using PaymentCore;

namespace Payment.JDPay;

internal class Definition
{
    public const string CallbackResponse = "SUCCESS";
    public const string SuccessStatus = "success";

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
    public const string CreateOrder = "/api/Pay/orderadd";

    public const string CreatePayout = "/api/Pay/dfadd";
}