using PaymentCore;

namespace Payment.TianHongPay;

public class Definition
{
    private static Dictionary<string, TransStatus> StatusMap =>
        new()
        {
            { "RE", TransStatus.Pending },
            { "SU", TransStatus.Success },
            { "FA", TransStatus.Fail }
        };
    
    public static TransStatus GetTransStatus(string status)
        => StatusMap.GetValueOrDefault(status, TransStatus.Pending);
}

internal static class CallbackResponse
{
    public const string Success = "ok";
    public const string Failed = "failed";
}

internal class Path
{
    public const string CreateOrder = "/payorder";
}