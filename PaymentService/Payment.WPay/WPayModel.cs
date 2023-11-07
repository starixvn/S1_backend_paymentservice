using Newtonsoft.Json;

namespace Payment.WPay;

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
    public int Code { get; set; }

    [JsonProperty("text")]
    public string ErrorMessage { get; set; }

    /// <summary>
    /// WPay交易單號
    /// </summary>
    [JsonProperty("tradeNo")]
    public string TradeNo { get; set; }

    [JsonProperty("targetUrl")]
    public string TargetUrl { get; set; }

    [JsonProperty("qrcode")]
    public string QrCode { get; set; }

    /// <summary>
    /// 附言
    /// </summary>
    [JsonProperty("kfNo")]
    public string KfNo { get; set; }
}

public class DepositCallBackRequest
{
    public string status { get; set; }

    public string tradeNo { get; set; }

    public string orderNo { get; set; }

    public string userNo { get; set; }

    public string userName { get; set; }

    public string channelNo { get; set; }

    public string amount { get; set; }

    public string discount { get; set; }

    public string lucky { get; set; }

    /// <summary>
    /// 實際金額
    /// </summary>
    public decimal paid { get; set; }

    public string extra { get; set; }

    public string sign { get; set; }
}

public class CreatPayoutResponse
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("text")]
    public string ErrorMessage { get; set; }

    [JsonProperty("tradeNo")]
    public string TradeNo { get; set; }

    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }
}

public class WithdrawalCallBackRequest
{
    public string status { get; set; }

    public string tradeNo { get; set; }

    public string orderNo { get; set; }

    public string amount { get; set; }

    public string name { get; set; }

    public string bankName { get; set; }

    public string bankAccount { get; set; }

    public string bankBranch { get; set; }

    public string memo { get; set; }

    public string mobile { get; set; }

    public string fee { get; set; }

    public string extra { get; set; }

    public string sign { get; set; }
}