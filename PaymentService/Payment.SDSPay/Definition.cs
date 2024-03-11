namespace Payment.SDSPay;

public class Definition
{
    
}

internal static class ErrorCode
{
    public const string Success = "0";
}

internal static class Path
{
    public const string Deposit = "/api/trade_order";
    public const string Payout = "/api/transfer";
    public const string ExchangeRate = "/api/exchange_rate";
}