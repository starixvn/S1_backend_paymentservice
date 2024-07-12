using Newtonsoft.Json;

namespace Payment.S88Pay;

public class CallbackRequest
{
    [JsonProperty("orderStatus")]
    public int Status { get; set; }
    
    [JsonProperty("orderNo")]
    public string ThirdId { get; set; }
    
    [JsonProperty("merOrderNo")]
    public string TransId { get; set; }
    
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
    
    [JsonProperty("sign")]
    public string Sign { get; set; }
}

public class WithdrawalResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}
