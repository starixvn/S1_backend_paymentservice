namespace PaymentCore.Model;

public interface IPay
{
    int PayId { get; set; }
    int ChannelId { get; set; }
    int CurrencyId { get; set; }
    string BankCode { get; set; }
}

public class Pay
{
    public int PayId { get; set; }
    public string PayName { get; set; }
    public string Setting { get; set; }
}