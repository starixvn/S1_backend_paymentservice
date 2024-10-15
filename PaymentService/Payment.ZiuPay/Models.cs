using Newtonsoft.Json;

namespace Payment.ZiuPay;

internal class ZiuPayResp<T>
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
    
    // 用戶存入金額一定要和申請金額相符才會回調成功，他們的actualAmount是扣掉商戶手續費的金額因此不能用它上分
    [JsonProperty("requestAmount")]
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
