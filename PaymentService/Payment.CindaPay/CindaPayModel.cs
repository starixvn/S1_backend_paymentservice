using Newtonsoft.Json;

namespace Payment.CindaPay;

public class CreatOrderResponse
{
    [JsonProperty("retCode")]
    public string Code { get; set; }
    
    [JsonProperty("retMsg")]
    public string Message { get; set; }
    
    [JsonProperty("link")]
    public string Link  { get; set; }
    
    [JsonProperty("rockTradeNo")]
    public string ThirdId  { get; set; }
}

public class CreatPayoutResponse
{
    [JsonProperty("retCode")]
    public string Code { get; set; }
    
    [JsonProperty("retMsg")]
    public string Message { get; set; }
    
    [JsonProperty("rockTradeNo")]
    public string ThirdId  { get; set; }
    
    [JsonProperty("status")]
    public string Status  { get; set; }
}