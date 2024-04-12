namespace Payment.TengLongPay;

internal class Definition
{
    
}

internal static class CallbackResponse
{
    public const string Success = "success";
    public const string Failed = "failed";
}

internal class Path
{
    public const string CreateOrder = "/api/index/orderdoing";

    public const string QueryOrder = "/api/index/getOrderStatus";
    
    public const string CreatePayout = "";
}