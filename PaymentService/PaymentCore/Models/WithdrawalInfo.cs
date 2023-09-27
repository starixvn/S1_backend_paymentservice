namespace PaymentCore.Models;

public class CryptoRateInfo
{
    public decimal ExchangeRate { get; set; }
    public decimal ExchangeAmount { get; set; }
    public decimal TempExchangeRate { get; set; }
    public decimal TempExchangeAmount { get; set; }
}