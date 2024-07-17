using Newtonsoft.Json;

namespace Payment.S88Pay;

public class CallbackRequest
{
    [JsonProperty("key")]
    public string EncryptData { get; set; }
    
    [JsonProperty("transaction_no")]
    public string ThirdId { get; set; }
    
    [JsonProperty("transaction_code")]
    public string TransId { get; set; }
}

public class WithdrawalResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}
