using Newtonsoft.Json;

namespace Payment.TengLongPay;

internal class DepositResp
{
    [JsonProperty("code")]
    public int Code { get; set; }
    
    [JsonProperty("data")]
    public string Data { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
}

internal class WithdrawResp
{
    [JsonProperty("status")]
    public string Status { get; set; }
    
    [JsonProperty("transaction_id")]
    public string ThirdId { get; set; }
    
    [JsonProperty("out_trade_no")]
    public string TransId { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
}

