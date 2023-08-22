using Newtonsoft.Json;

namespace Payment.WPPay;

internal class MediaType
{
    public const string Json = "application/json";
}


/// <summary>
/// 充值(Debit)、代付(Credit) 建單回覆
/// </summary>
public class WPPayCreateResponse
{
    [JsonProperty("status")]
    public string? Status { get; set; }

    [JsonProperty("errorCode")]
    public string? ErrorCode { get; set; }

    [JsonProperty("msg")]
    public string? Msg { get; set; }

    [JsonProperty("data")]
    public string? Data { get; set; }
}

/// <summary>
/// 充值(Debit)、代付(Credit) 建單Data
/// </summary>
public class WPPayCreateDataResponse
{
    [JsonProperty("merchantCode")]
    public string? MerchantCode { get; set; }

    [JsonProperty("content")]
    public string? Content { get; set; }
}

public class WPPayCreateDataContent
{
    [JsonProperty("merchantCode")]
    public string? MerchantCode { get; set; }

    [JsonProperty("merchantTradeNo")]
    public string? MerchantTradeNo { get; set; }

    [JsonProperty("tradeNo")]
    public string? TradeNo { get; set; }

    [JsonProperty("payUrl")]
    public string? PayUrl { get; set; }

    [JsonProperty("sign")]
    public string? Sign { get; set; }
}

public class WPPayPayoutDataContent
{
    [JsonProperty("merchantCode")]
    public string? MerchantCode { get; set; }

    [JsonProperty("merchantTradeNo")]
    public string? MerchantTradeNo { get; set; }

    [JsonProperty("tradeNo")]
    public string? TradeNo { get; set; }

    [JsonProperty("tradeStatus")]
    public string? TradeStatus { get; set; }

    [JsonProperty("sign")]
    public string? Sign { get; set; }
}

/// <summary>
/// 代收(Debit)、代付(Credit) 異步回調請求
/// </summary>
public class WPPayCallBackRequest
{
    [JsonProperty("merchantCode")]
    public string? MerchantCode { get; set; }

    [JsonProperty("content")]
    public string? Content { get; set; }
}

internal class CallbackResponse
{
    public const string Success = "SUCCESS";
    public const string Failed = "FAIL";
}