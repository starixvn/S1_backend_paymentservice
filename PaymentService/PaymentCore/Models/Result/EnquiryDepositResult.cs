using PaymentCore.Interface;

namespace PaymentCore.Models.Result;

public class EnquiryDepositResult : IDepositResult
{
    public TransStatus TransStatus { get; set; }
    public string Message { get; set; }
    public string DepositId { get; set; }
    public string ThirdId { get; set; }
    public decimal RequestAmount { get; set; }
    public decimal ActualAmount { get; set; }
    public decimal Fee { get { return RequestAmount - ActualAmount; } }
    public string Comment { get; set; }
    public string RawContent { get; set; }
    public string RefThirdId { get; set; }
}