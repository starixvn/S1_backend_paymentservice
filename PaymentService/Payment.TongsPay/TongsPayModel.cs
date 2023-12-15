using Newtonsoft.Json;

namespace Payment.TongsPay;


internal class DepositCallbackRequest
{
    
    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("successAmt")]
    public decimal Amount { get; set; }

    [JsonProperty("orderStatus")]
    public string Status { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}

internal class WithdrawalResponse
{
    [JsonProperty("recode")]
    public string Code { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }
}

internal class WithdrawalCallbackRequest
{
    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("recode")]
    public string Code { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}