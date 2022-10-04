namespace PaymentCore.Interface;

public interface IWithdrawResult
{
    public TransStatus TransStatus { get; set; }
    public string Message { get; set; }
    public string WithdrawId { get; set; }
    public decimal RequestAmount { get; set; }
    public string ThirdId { get; set; }
}