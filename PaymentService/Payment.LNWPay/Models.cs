using Newtonsoft.Json;

namespace Payment.LNWPay;

internal class PayResp<T>
{
    [JsonProperty("status")]
    public string Status { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
    
    [JsonProperty("data")]
    public T Data { get; set; }
}

internal class AuthData
{
    [JsonProperty("token_type")]
    public string Type { get; set; }
    
    [JsonProperty("access_token")]
    public string Token { get; set; }
}

internal interface IDepositData
{
    public decimal Amount { get; set; }
    
    public string Url { get; set; }
    
    public string ThirdId { get; set; }
}

internal class DepositReq
{
    [JsonProperty("requestTime")]
    public string RequestTime
        => DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
    
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("clientAccount")]
    public string ClientAccount { get; set; }
    
    [JsonProperty("clientAccountName")]
    public string ClientAccountName { get; set; }
    
    [JsonProperty("reference1")]
    public string TransId { get; set; }
}

internal class PayoutReq
{
    [JsonProperty("requestTime")]
    public string RequestTime
        => DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss");
    
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("clientAccount")]
    public string ClientAccount { get; set; }
    
    [JsonProperty("clientAccountName")]
    public string ClientAccountName { get; set; }
    
    [JsonProperty("clientBank")]
    public string ClientBank { get; set; }
    
    [JsonProperty("reference1")]
    public string TransId { get; set; }
}

internal class PayoutData
{
    [JsonProperty("requestId")]
    public string ThirdId { get; set; }
    
    [JsonProperty("reference1")]
    public string TransId { get; set; }
    
    [JsonProperty("accountName")]
    public string AccountName { get; set; }
    
    [JsonProperty("accountNo")]
    public string AccountNo { get; set; }
    
    [JsonProperty("bankCode")]
    public string BankCode { get; set; }
    
    [JsonProperty("bankName")]
    public string BankName { get; set; }
    
    [JsonProperty("amount")]
    public string Amount { get; set; }
    
    [JsonProperty("wdStatus")]
    public string Status { get; set; }
}

internal class DepositDataAuto : IDepositData
{
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("accountURL")]
    public string Url { get; set; }
    
    [JsonProperty("requestId")]
    public string ThirdId { get; set; }
}

internal class DepositDataQr : IDepositData
{
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("qrcodeURL")]
    public string Url { get; set; }
    
    [JsonProperty("requestId")]
    public string ThirdId { get; set; }
}

internal class DepositDataTrue : IDepositData
{
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("walletURL")]
    public string Url { get; set; }
    
    [JsonProperty("requestId")]
    public string ThirdId { get; set; }
}