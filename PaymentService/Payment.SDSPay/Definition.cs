namespace Payment.SDSPay;

internal class Definition
{
    public const string SuccessCode = "0";
}

internal static class Path
{
    public const string Deposit = "/api/trade_order";
    public const string Payout = "/api/transfer";
    public const string ExchangeRate = "/api/exchange_rate";
}