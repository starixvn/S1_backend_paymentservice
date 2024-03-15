using Newtonsoft.Json;

namespace Payment.SDSPay;

public class ExchangeRate
{
    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
    
    [JsonProperty("rate")]
    public decimal Rate { get; set; }
}