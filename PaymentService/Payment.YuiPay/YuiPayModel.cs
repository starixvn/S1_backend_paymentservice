using Newtonsoft.Json;

namespace Payment.YuiPay;

internal class DepositResponse
{
    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }

    [JsonProperty("detail")]
    public DepositDetail Detail { get; set; }
}

internal class DepositDetail
{
    [JsonProperty("PayURL")]
    public string PayUrl { get; set; }
}

internal class DepositCallbackRequest
{
    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("bizAmt")]
    public decimal Amount { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}

internal class WithdrawalResponse
{
    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
}

internal class WithdrawalCallbackRequest
{
    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("bizAmt")]
    public decimal Amount { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }
    
    [JsonProperty("remark")]
    public string Remark { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}