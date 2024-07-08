using Newtonsoft.Json;

namespace Payment.OneDayPay;

public class OneDayPayRequest
{
    public string MerchantCode { get; set; }

    public string Data { get; set; }
    
    public string Signature { get; set; }
    
    public int Version { get; set; }
}

public class Response<T>
{
    [JsonProperty("isSuccess")]
    public bool IsSuccess { get; set; }
    
    [JsonProperty("successCode")]
    public int Code { get; set; }
    
    [JsonProperty("errorMessage")]
    public string Message { get; set; }
    
    [JsonProperty("data")]
    public T Data { get; set; }
}

public class CreateDeposit
{
    [JsonProperty("url")]
    public string Url { get; set; }
    
    [JsonProperty("transactionId")]
    public string ThirdId { get; set; }
}

public class CallbackRequest
{
    [JsonProperty("TransactionId")]
    public string ThirdId { get; set; }
    
    [JsonProperty("MerchantTransactionId")]
    public string TransId { get; set; }
    
    public string Data { get; set; }
    
    public string Signature { get; set; }
}

public class CallbackData
{
    public string Status { get; set; }
    
    public decimal Amount { get; set; }
}
public class CreatePayout
{
    [JsonProperty("transactionId")]
    public string ThirdId { get; set; }
}