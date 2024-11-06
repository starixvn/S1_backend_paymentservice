using Newtonsoft.Json;

namespace Payment._808Pay;

public class Response<T>
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

    [JsonProperty("orderNo")]
    public string OrderNo { get; set; }
}

public class CreatePayoutData
{
    [JsonProperty("orderNo")]
    public string OrderNo { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
}

public class ReceiveModel
{
    [JsonProperty("appId")]
    public string AppId { get; set; }

    [JsonProperty("timestamp")]
    public string TimeStamp { get; set; }

    [JsonProperty("nonce")]
    public string Nonce { get; set; }

    [JsonProperty("orderNo")]
    public string OrderNo { get; set; }

    [JsonProperty("mchNo")]
    public string MchNo { get; set; }

    [JsonProperty("mchOrderNo")]
    public string MchOrderNo { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }

    [JsonProperty("receiveAccount")]
    public string ReceiveAccount { get; set; }
}
