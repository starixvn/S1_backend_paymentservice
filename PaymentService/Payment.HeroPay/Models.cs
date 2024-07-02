using Newtonsoft.Json;

namespace Payment.HeroPay;

public class HeroPayResponse
{
    [JsonProperty("retCode")]
    public string Code { get; set; }
    
    [JsonProperty("retMsg")]
    public string Message { get; set; }
    
    [JsonProperty("payOrderId")]
    public string DepositThirdId { get; set; }
    
    [JsonProperty("transOrderId")]
    public string PayoutThirdId { get; set; }

    [JsonProperty("payParams")]
    public string JsonData { get; set; }
}

public class CreateDeposit
{
    [JsonProperty("payUrl")]
    public string Url { get; set; }
}

public class DepositCallbackRequest
{
    [JsonProperty("payOrderId")]
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

public class PayoutCallbackRequest
{
    [JsonProperty("transOrderId")]
    public string ThirdId { get; set; }
    
    [JsonProperty("mchTransOrderNo")]
    public string TransId { get; set; }
    
    [JsonProperty("amount")]
    public string Amount { get; set; }
    
    [JsonProperty("status")]
    public string Status { get; set; }
    
    [JsonProperty("sign")]
    public string Sign { get; set; }
}