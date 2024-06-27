namespace PaymentCore.Interface;

public interface IPayHelperNoCallBackUrl
{
    string FetchTransId(Dictionary<string, string> formData, string bodyContent, Dictionary<string, string> payResource);
}