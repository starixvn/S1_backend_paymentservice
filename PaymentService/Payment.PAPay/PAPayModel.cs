using Newtonsoft.Json;

namespace Payment.PAPay;

public class PAPayResponse
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}

public class PAPayCreateOrderResponse : PAPayResponse
{
    [JsonProperty("order_id")]
    public string OrderId { get; set; }

    [JsonProperty("pay_url")]
    public string PayUrl { get; set; }
    [JsonProperty("channel_id")]
    public string ChannelId { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }

}

public class DepositCallBackRequest
{

    public string mch_id { get; set; }

    public string order_id { get; set; }

    public string out_trade_no { get; set; }

    public string traid { get; set; }

    public string pay_type { get; set; }

    public string attach { get; set; }

    public string total_fee { get; set; }

    public string code { get; set; }

    public string code_msg { get; set; }

    public string sign { get; set; }
}

internal class CallbackResponse
{
    public const string Success = "ok";
    public const string Failed = "failed";
}