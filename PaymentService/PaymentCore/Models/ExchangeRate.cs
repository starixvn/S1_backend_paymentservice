namespace PaymentCore.Models;

public class ExchangeRate
{
    public long Updated_timestamp { get; set; }
    public decimal BuyRate { get; set; }
    public decimal SellRate { get; set; }
}