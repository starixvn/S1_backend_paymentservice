using PaymentCore;

namespace Payment.SDSPay;

internal class Definition
{
    private static Dictionary<string, TransStatus> PayoutStatusMap =>
        new()
        {
            { "1", TransStatus.Pending },
            { "2", TransStatus.Success },
            { "3", TransStatus.Fail }
        };
    
    public static TransStatus GetPayoutTransStatus(string status)
        => PayoutStatusMap.GetValueOrDefault(status, TransStatus.Pending);
    
    public const string SuccessCode = "200";
}

internal static class CallbackResponse
{
    public const string Success = "success";
    public const string Failed = "failed";
}

internal static class Path
{
    public const string Deposit = "/api/trade_order";
    public const string Payout = "/api/transfer";
    public const string ExchangeRate = "/api/exchange_rate";
}