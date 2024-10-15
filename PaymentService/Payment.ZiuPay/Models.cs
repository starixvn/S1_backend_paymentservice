using Newtonsoft.Json;

namespace Payment.ZuiPay;

internal class ZuiPayResp<T>
{
    [JsonProperty("code")]
    public int Code { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }

    [JsonProperty("data")]
    public T Data { get; set; }
}

internal class DepositRespData
{
    [JsonProperty("oder_no")]
    public string ThirdId { get; set; }

    [JsonProperty("redirectUrl")]
    public string RedirectUrl { get; set; }
}

internal class CallbackReq
{
    [JsonProperty("order_no")]
    public string TransId { get; set; }
    
    [JsonProperty("status")]
    public string Status { get; set; }
    
    [JsonProperty("actualAmount")]
    public decimal ActualAmount { get; set; }
}

internal class CallbackResp
{
    public CallbackResp(bool success)
    {
        Success = success;
    }
    
    [JsonProperty("success")]
    public bool Success { get; set; }
}
