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

internal class GetAddressData
{
    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
    
    [JsonProperty("address")]
    public string Address { get; set; }

    [JsonProperty("qrCode")]
    public string QrCode { get; set; }
}
