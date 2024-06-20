using Newtonsoft.Json;

namespace Payment.PapayaPay;

public class DepositResponse
{
    [JsonProperty("statusCode")]
    public string StatusCode { get; set; }
    
    [JsonProperty("payurl")]
    public string Url { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}

public class WithdrawalResponse
{
    [JsonProperty("message")]
    public string Message { get; set; }
    
    [JsonProperty("data")]
    public object Data { get; set; }
}

public class DepositCallbackRequest
{
    [JsonProperty("status")]
    public string Status { get; set; }
    
    [JsonProperty("callbackStatus")]
    public string CallbackStatus { get; set; }
    
    [JsonProperty("qrCodeTransactionId")]
    public string TransId { get; set; }
    
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("id")]
    public string ThirdId { get; set; }
}

public class DepositCallbackResponse
{
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonProperty("qrCodeTransactionId")]
    public string TransId { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}

public class PayoutCallbackRequest
{
    [JsonProperty("fundOutStatus")]
    public string Status { get; set; }
    [JsonProperty("fundOutPaymentReference")]
    public string TransId { get; set; }
    
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("transactionRef1")]
    public string ThirdId { get; set; }
}

public class PayoutCallbackResponse
{
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}
