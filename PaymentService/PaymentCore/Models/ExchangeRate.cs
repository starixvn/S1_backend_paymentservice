namespace PaymentCore.Models;

public class ExchangeRate
{
    public long Updated_timestamp { get; set; }
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
}

public class CryptoExchangeInfo
{
    public decimal Rate { get; set; }
    public decimal ExchangeAmount { get; set; }
    public long Timestamp { get; set; }
}