using Newtonsoft.Json;

namespace Payment.KzPay;

public class KzPayResponse<T>
{
    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public T Data { get; set; }
}

public class CreateDeposit
{
    [JsonProperty("sysOrderNo")]
    public string ThirdId { get; set; }
    
    [JsonProperty("payUrl")]
    public string Url { get; set; }
}

public class CallbackRequest
{
    [JsonProperty("sysOrderNo")]
    public string ThirdId { get; set; }
    
    [JsonProperty("mchOrderNo")]
    public string TransId { get; set; }
    
    [JsonProperty("amount")]
    public string Amount { get; set; }
    
    [JsonProperty("status")]
    public string Status { get; set; }
    
    [JsonProperty("sign")]
    public string Sign { get; set; }
}