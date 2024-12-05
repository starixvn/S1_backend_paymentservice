using Newtonsoft.Json;

namespace Payment.LuckyPay;

public class CreatOrderResp
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("data")]
    public CreatOrderRespData Data { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}

public class CreatOrderRespData
{
    [JsonProperty("orderNo")]
    public string OrderNo { get; set; }

    [JsonProperty("receiveAddr")]
    public string ReceiveAddr { get; set; }

    [JsonProperty("chainName")]
    public string ChainName { get; set; }

    [JsonProperty("coinUnit")]
    public string CoinUnit { get; set; }

    [JsonProperty("requestAmount")]
    public decimal RequestAmount { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("payUrl")]
    public string PayUrl { get; set; }

    [JsonProperty("hrefbackurl")]
    public string Hrefbackurl { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}

public class CreatPayoutResp
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("data")]
    public CreatPayoutRespData Data { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}

public class CreatPayoutRespData
{
    [JsonProperty("orderNo")]
    public string OrderNo { get; set; }

    [JsonProperty("receiveAddr")]
    public string ReceiveAddr { get; set; }

    [JsonProperty("chainName")]
    public string ChainName { get; set; }

    [JsonProperty("coinUnit")]
    public string CoinUnit { get; set; }

    [JsonProperty("requestAmount")]
    public decimal RequestAmount { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}