using Newtonsoft.Json;

namespace Payment.PattayaPay;

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

public class PattayaPayResponse<T>
{
    [JsonProperty("code")]
    public int Code { get; set; }
    
    [JsonProperty("results")]
    public T Results { get; set; }
}

public class CreatOrderResults
{
    [JsonProperty("tradeno")]
    public string TradeNo { get; set; }
    
    [JsonProperty("redirect")]
    public string TargetUrl { get; set; }
}

public class DepositCallBackRequest
{
    public string tradeno { get; set; }
    
    public string outtradeno { get; set; }
    
    public decimal amount { get; set; }
    
    public decimal ramount { get; set; }
    
    public decimal oamount { get; set; }
    
    public string endtime { get; set; }
    
    public string status { get; set; }
    
    public string remark { get; set; }
    
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
    public string transferno { get; set; }
    
    public string outtransferno { get; set; }
    
    public decimal tradeamount { get; set; }
    
    public decimal transferamount { get; set; }
    
    public string endtime { get; set; }
    
    public string remark { get; set; }
    
    public string status { get; set; }
    
    public string sign { get; set; }
}