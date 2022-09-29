namespace PaymentCore.Interface;

public interface IDepositResult
{
    TransStatus TransStatus { get; set; }
    string Message { get; set; }
    string DepositId { get; set; }
    string ThirdId { get; set; }
    decimal RequestAmount { get; set; }
    decimal ActualAmount { get; set; }
    decimal Fee { get; }
    string Comment { get; set; }
    string RefThirdId { get; set; }
}