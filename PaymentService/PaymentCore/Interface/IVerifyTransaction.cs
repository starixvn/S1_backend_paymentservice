using PaymentCore.Models.Result;

namespace PaymentCore.Interface;

public interface IVerifyTransaction
{
    public (string TransId, IDictionary<string, string> Data) FetchTransIdAndDataBeforeVerify(
        IDictionary<string, string> formData, string bodyContent, Dictionary<string, string> headers);

    public VerifyTransactionResult VerifyTransaction(IDictionary<string, string> data,
        IDictionary<string, string> input);

}