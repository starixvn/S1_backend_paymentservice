using Newtonsoft.Json;

namespace Payment.ChangChenPay;

public class CreatOrderResponse
{
    [JsonProperty("retCode")]
    public string Code { get; set; }

    [JsonProperty("retMsg")]
    public string Message { get; set; }

    [JsonProperty("payOrderId")]
    public string ThirdId { get; set; }

    [JsonProperty("payParams")]
    public CreatOrderData Data { get; set; }
}

public class CreatOrderData
{
    [JsonProperty("payUrl")]
    public string Url { get; set; }
}

public class CreatPayoutResponse
{
    [JsonProperty("retCode")]
    public string Code { get; set; }

    [JsonProperty("retMsg")]
    public string Message { get; set; }

    [JsonProperty("payOrderId")]
    public string ThirdId { get; set; }
}