using Newtonsoft.Json;

namespace Payment.HonorPay;

internal class DepositResponse
{
    [JsonProperty("retCode")]
    public string ErrorCode { get; set; }
    
    [JsonProperty("retMsg")]
    public string ErrorMessage { get; set; }

    [JsonProperty("uuid")]
    public string TpTransId { get; set; }

    [JsonProperty("redirectURL")]
    public string RedirectUrl { get; set; }

    [JsonProperty("realCharge")]
    public string RealCharge { get; set; }
}

internal class DepositCallbackRequest
{
    /// <summary>
    /// 商戶號
    /// </summary>
    public string account { get; set; }

    public string nonceStr { get; set; }

    public string orderNo { get; set; }

    /// <summary>
    /// 申請金額
    /// </summary>
    public decimal payMoney { get; set; }

    /// <summary>
    /// 
    /// </summary>
    public string payStatus { get; set; }

    /// <summary>
    /// 實際金額
    /// </summary>
    public decimal realCharge { get; set; }

    public string uuid { get; set; }

    public string sign { get; set; }
}

internal class WithdrawalResponse
{
    [JsonProperty("retCode")]
    public string ErrorCode { get; set; }

    [JsonProperty("uuid")]
    public string TpTransId { get; set; }

    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("fee")]
    public string Fee { get; set; }
}

internal class WithdrawalCallbackRequest
{
    public string account { get; set; }

    public decimal amount { get; set; }

    public decimal fee { get; set; }

    public string orderNo { get; set; }

    public string nonceStr { get; set; }

    public string uuid { get; set; }

    public string status { get; set; }

    public string sign { get; set; }
}