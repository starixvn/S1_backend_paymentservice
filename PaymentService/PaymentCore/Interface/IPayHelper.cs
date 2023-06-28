using PaymentCore.Models.Result;

namespace PaymentCore.Interface;

public interface IPayHelper
{
    DepositResult Debit(Dictionary<string, string> inputs);
    WithdrawResult Credit(Dictionary<string, string> inputs);
    Dictionary<string, string> CheckHealthForChannels(Dictionary<string, string> inputs);

    event InsertPaymentLogEventHandler InsertPaymentLog;
    EnquiryDepositResult EnquiryForDebit(Dictionary<string, string> inputs);
    EnquiryWithdrawResult EnquiryForCredit(Dictionary<string, string> inputs);
    NotifyDepositResult CallBackDebit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource);
    NotifyWithdrawResult CallBackCredit(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers, Dictionary<string, string> payResource);
}