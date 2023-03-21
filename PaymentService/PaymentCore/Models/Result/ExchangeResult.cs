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

    public ExchangeResult(string errorCode, string message, CryptoExchangeInfo formData)
    {
        this.ErrorCode = errorCode;
        this.Message = message;
        this.ExchangeInfo = formData;
    }

    public string Message { get; set; }
    public CryptoExchangeInfo ExchangeInfo { get; set; }
    public string ErrorCode { get; set; }
}