using Newtonsoft.Json;

namespace Payment.IMPay;

internal class PayResp<T>
{
    [JsonProperty("status")]
    public bool Status { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
    
    [JsonProperty("data")]
    public T Data { get; set; }
    
    [JsonProperty("response")]
    public ErrorResp Response { get; set; }
}

internal class ErrorResp
{
    [JsonProperty("statusCode")]
    public string StatusCode { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}

internal class DepositCallbackReq
{
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("transactionId")]
    public string ThirdId { get; set; }
    
    [JsonProperty("status")]
    public string Status { get; set; }
}

internal class DepositData
{
    [JsonProperty("transactionId")]
    public string ThirdId { get; set; }
    
    [JsonProperty("url")]
    public string Url { get; set; }
}

internal class PayoutCallbackReq
{
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("transactionId")]
    public string ThirdId { get; set; }
    
    [JsonProperty("status")]
    public string Status { get; set; }
}