using Newtonsoft.Json;

namespace Payment.EBPay;

public class EBPayResp<T>
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public T Data { get; set; }
}

public class CreateOrderData
{
    [JsonProperty("url")]
    public string Url { get; set; }
}

public class CreatePayoutData
{
    [JsonProperty("orderId")]
    public string ThirdId { get; set; }
}

internal class CallbackResponse
{
    public CallbackResponse(int code, string msg)
    {
        Code = code;
        Message = msg;
    }
    
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }
}