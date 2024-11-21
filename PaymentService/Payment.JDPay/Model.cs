using Newtonsoft.Json;

namespace Payment.JDPay;

public class CreatOrderResponse
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }
    
    /// <summary>
    /// bankCode为1 支持手机电脑端跳转
    /// bankCode不为1 只支持电脑
    /// bankCode为5 此参数为账号 其余参数不传
    /// </summary>
    [JsonProperty("payurl")]
    public string Url1 { get; set; }

    /// <summary>
    /// bankCode为1 支持手机电脑端跳
    ///转 bankCode不为1 只支持手机
    /// </summary>
    [JsonProperty("payurls")]
    public string Url2 { get; set; }
}

public class CreatPayoutResponse
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }
}

public class CreatPayoutData
{
    [JsonProperty("trade_no")]
    public string TransId { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("out_trade_no")]
    public string ThirdId { get; set; }

    [JsonProperty("bank_id")]
    public string BankId { get; set; }

    [JsonProperty("account_number")]
    public string AccountNumber { get; set; }

    [JsonProperty("bank_owner")]
    public string BankOwner { get; set; }

    [JsonProperty("callback_url")]
    public string CallbackUrl { get; set; }

    [JsonProperty("state")]
    public string State { get; set; }
}
