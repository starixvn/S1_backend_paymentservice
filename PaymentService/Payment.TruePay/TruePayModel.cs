using Newtonsoft.Json;

namespace Payment.TruePay;

internal class DepositResp
{
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
    
    [JsonProperty("d")]
    public DepositRespData Data { get; set; }
}

internal class DepositRespData
{
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("token")]
    public string Token { get; set; }
    
    [JsonProperty("url")]
    public string Url { get; set; }

}

internal class DepositCallbackReq
{
    [JsonProperty("id")]
    public string TransId { get; set; }
    
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
    
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("fee")]
    public decimal Fee { get; set; }
    
    [JsonProperty("token")]
    public string Token { get; set; }
    
    [JsonProperty("ts")]
    public string Timestamp { get; set; }
}

internal class WithdrawalResp
{
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
    
    [JsonProperty("d")]
    public WithdrawalRespData Data { get; set; }
}

internal class WithdrawalRespData
{
    [JsonProperty("id")]
    public string Id { get; set; }
    
    [JsonProperty("trxid")]
    public string TransId { get; set; }
}