using Newtonsoft.Json;

namespace Payment.LionPay;

public enum OrderTypeEnum
{
    Payin = 0,
    Payout = 1
}

public class CreatOrderRequest
{
    [JsonProperty("merchantNo")]
    public string MerchantNo { get; set; }

    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("userName")]
    public string UserName { get; set; }

    [JsonProperty("channelNo")]
    public string ChannelNo { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("amountBeforeFixed")]
    public decimal AmountBeforeFixed { get; set; }

    [JsonProperty("payeeName")]
    public string PayeeName { get; set; }

    [JsonProperty("bankName")]
    public string BankName { get; set; }

    [JsonProperty("datetime")]
    public string DateTime { get; set; }

    [JsonProperty("notifyUrl")]
    public string NotifyUrl { get; set; }

    [JsonProperty("time")]
    public long Timestamp { get; set; }

    [JsonProperty("appSecret")]
    public string AppSecret { get; set; }
}

public class CreatOrderResponse
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    /// <summary>
    /// LionPay交易單號
    /// </summary>
    [JsonProperty("platformOrderNo")]
    public string PlatformOrderNo { get; set; }

    [JsonProperty("orderAmount")]
    public decimal OrderAmount { get; set; }

    [JsonProperty("orderFee")]
    public decimal Fee { get; set; }

    [JsonProperty("depositUrl")]
    public string DepositUrl { get; set; }
}

public class CreatPayoutResponse
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("merchantOrderNo")]
    public string TransId { get; set; }

    /// <summary>
    /// LionPay交易單號
    /// </summary>
    [JsonProperty("platformOrderNo")]
    public string PlatformOrderNo { get; set; }

    [JsonProperty("orderAmount")]
    public decimal OrderAmount { get; set; }

    [JsonProperty("orderFee")]
    public decimal Fee { get; set; }
}

public class CallBackRequest
{
    [JsonProperty("merchantCode")]
    public string MerchantCode { get; set; }

    [JsonProperty("signType")]
    public string SignType { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("merchantOrderNo")]
    public string TransId { get; set; }

    [JsonProperty("platformOrderNo")]
    public string PlatformOrderNo { get; set; }

    [JsonProperty("orderAmount")]
    public decimal OrderAmount { get; set; }

    [JsonProperty("actualAmount")]
    public decimal ActualAmount { get; set; }

    [JsonProperty("actualFee")]
    public decimal ActualFee { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }

    [JsonProperty("orderStatus")]
    public string OrderStatus { get; set; }
}