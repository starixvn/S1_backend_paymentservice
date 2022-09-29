using PaymentCore.Interface;

namespace PaymentCore.Models.Result;

public class NotifyWithdrawResult : IWithdrawResult
{
    public TransStatus TransStatus { get; set; }
    public string Message { get; set; }
    public string WithdrawId { get; set; }
    public decimal RequestAmount { get; set; }
    //public decimal Fee { get { return OrderAmount - FactAmount; } }
    public string ThirdId { get; set; }
    public string Response { get; set; }
}