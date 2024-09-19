using Newtonsoft.Json;

namespace Payment.RapidPay;

internal class CallBackCreditReq
{
    [JsonProperty("order_id")]
    public string TransId { set; get; }
    
    [JsonProperty("amount")]
    public string Amount { set; get; }
    
    [JsonProperty("currency")]
    public string Currency { set; get; }
    
    [JsonProperty("order_status")]
    public string OrderStatus { set; get; }
    
    [JsonProperty("status")]
    public bool Status { set; get; }
    
    [JsonProperty("token")]
    public string Token { set; get; }
    
    [JsonProperty("charge")]
    public string Charge { set; get; }
    
    [JsonProperty("name")]
    public string Name { get; set; }
    
    [JsonProperty("remarks")]
    public string Remarks { get; set; }
}

internal class CallBackDebitReq
{
    [JsonProperty("order_id")]
    public string TransId { set; get; }
    
    [JsonProperty("amount")]
    public string Amount { set; get; }
    
    [JsonProperty("currency")]
    public string Currency { set; get; }
    
    [JsonProperty("order_status")]
    public string OrderStatus { set; get; }
    
    [JsonProperty("payment_type")]
    public string PaymentType { set; get; }
    
    [JsonProperty("status")]
    public bool Status { set; get; }
    
    [JsonProperty("token")]
    public string Token { set; get; }
    
    [JsonProperty("charge")]
    public string Charge { set; get; }
}

internal class WithdrawResp
{
    [JsonProperty("status")]
    public bool Status { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}


internal class AuthResp
{
    [JsonProperty("status")]
    public bool Status { get; set; }
    
    [JsonProperty("auth")]
    public string Auth { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}

internal class CurrencyResp
{
    [JsonProperty("status")]
    public bool Status { get; set; }
    
    [JsonProperty("rate")]
    public IEnumerable<Limit> Limit { get; set; }
}
internal class Limit
{
    [JsonProperty("currency")]
    public string Currency { get; set; }
    
    [JsonProperty("min")]
    public decimal Min { get; set; }
    
    [JsonProperty("max")]
    public decimal Max { get; set; }
}

internal class DepositResp
{
    [JsonProperty("status")]
    public bool Status { get; set; }
    
    [JsonProperty("p_url")]
    public string Url { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}