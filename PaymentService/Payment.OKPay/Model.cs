using Newtonsoft.Json;

namespace Payment.OKPay;

public class OKPayResp<T>
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public T Data { get; set; }
}

public class CreatOrderData
{
    [JsonProperty("orderNo")]
    public string ThirdId { get; set; }
    
    [JsonProperty("payUrl")]
    public string PayUrl { get; set; }

}

public class CreatPayoutData
{
    [JsonProperty("orderNo")]
    public string ThirdId { get; set; }
}
