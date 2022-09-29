namespace PaymentCore.Models.Result;

public class ExchangeResult
{
    public ExchangeResult()
    {
    }

    public ExchangeResult(string errorCode)
        : this(errorCode, null, null)
    {
    }

    public ExchangeResult(string errorCode, string message)
        : this(errorCode, message, null)
    {
    }

    public ExchangeResult(string errorCode, string message, Dictionary<string, string> formData)
    {
        this.ErrorCode = errorCode;
        this.Message = message;
        this.FormData = formData;
    }

    public string Message { get; set; }
    public Dictionary<string, string> FormData { get; set; }
    public string ErrorCode { get; set; }
}