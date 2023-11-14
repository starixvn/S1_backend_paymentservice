namespace PaymentCore.Models.Result;

public class VerifyTransactionResult
{
    public VerifyTransactionResult() { }

    public VerifyTransactionResult(string contentType, string response)
    {
        ContentType = contentType;
        Response = response;
    }

    public string ContentType { get; set; }
    public string Response { get; set; }
}