using Newtonsoft.Json;

namespace Payment.WPPay;


/// <summary>
/// 充值(Debit)、代付(Credit) 建單回覆
/// </summary>
public class WPPayCreateResponse
{
    /// <summary>
    /// 响应状态：SUCCESS 或 FAIL
    /// </summary>
    [JsonProperty("status")]
    public string? Status { get; set; }
    /// <summary>
    /// 错误编码
    /// </summary>
    [JsonProperty("errorCode")]
    public string? ErrorCode { get; set; }
    /// <summary>
    /// 状态描述
    /// </summary>
    [JsonProperty("msg")]
    public string? Msg { get; set; }
    /// <summary>
    /// 响应数据（json字符串）
    /// </summary>
    [JsonProperty("data")]
    public string? Data { get; set; }
}

/// <summary>
/// 充值(Debit)、代付(Credit) 建單回覆的Data
/// </summary>
public class WPPayCreateData
{
    /// <summary>
    /// 商户编码
    /// </summary>
    [JsonProperty("merchantCode")]
    public string? MerchantCode { get; set; }
    /// <summary>
    /// 加密内容
    /// </summary>
    [JsonProperty("content")]
    public string? Content { get; set; }
}

/// <summary>
/// 充值(Debit)、代付(Credit) 建單回覆的Data內文
/// </summary>
public class WPPayCreateDataContent
{
    /// <summary>
    /// 商户编码
    /// </summary>
    [JsonProperty("merchantCode")]
    public string? MerchantCode { get; set; }
    /// <summary>
    /// 商户订单号
    /// </summary>
    [JsonProperty("merchantTradeNo")]
    public string? MerchantTradeNo { get; set; }
    /// <summary>
    /// 平台订单号
    /// </summary>
    [JsonProperty("tradeNo")]
    public string? TradeNo { get; set; }
    /// <summary>
    /// 充值地址(充值(Debit)才有)
    /// </summary>
    [JsonProperty("payUrl")]
    public string? PayUrl { get; set; }
    /// <summary>
    /// RSA验证签名：支付平台公钥验证签名。除此字段外，其他字段按照签名规则 1.2.3 说明处理
    /// </summary>
    [JsonProperty("sign")]
    public string? Sign { get; set; }
}

/// <summary>
/// 代收(Debit)、代付(Credit) 異步回調請求
/// </summary>
public class WPPayCallBackRequest
{
    /// <summary>
    /// 商户编码
    /// </summary>
    [JsonProperty("merchantCode")]
    public string? MerchantCode { get; set; }
    /// <summary>
    /// 加密内容
    /// </summary>
    [JsonProperty("content")]
    public string? Content { get; set; }
}

/// <summary>
/// 充值(Debit)、代付(Credit) 異步回調內文
/// </summary>
public class WPPayCallBackDataContent
{
    /// <summary>
    /// 商户编码
    /// </summary>
    [JsonProperty("merchantCode")]
    public string? MerchantCode { get; set; }
    /// <summary>
    /// 商户订单号
    /// </summary>
    [JsonProperty("merchantTradeNo")]
    public string? MerchantTradeNo { get; set; }
    /// <summary>
    /// 平台订单号
    /// </summary>
    [JsonProperty("tradeNo")]
    public string? TradeNo { get; set; }
    /// <summary>
    /// 实际金额(充值(Debit)才有)
    /// </summary>
    [JsonProperty("payAmount")]
    public string? PayAmount { get; set; }
    /// <summary>
    /// 申请金额（保留两位小数），例如：”100.00”
    /// </summary>
    [JsonProperty("amount")]
    public string? Amount { get; set; }
    /// <summary>
    /// 交易状态
    /// </summary>
    [JsonProperty("tradeStatus")]
    public string? TradeStatus { get; set; }
    /// <summary>
    /// 通知时间：格式yyyyMMddHHmmss
    /// </summary>
    [JsonProperty("notifyTime")]
    public string? NotifyTime { get; set; }
    /// <summary>
    /// RSA验证签名：支付平台公钥验证签名。除此字段外，其他字段按照签名规则 1.2.3 说明处理
    /// </summary>
    [JsonProperty("sign")]
    public string? Sign { get; set; }
}

/// <summary>
/// 充值(Debit)、代付(Credit) 異步回覆
/// </summary>
internal class CallbackResponse
{
    public const string Success = "SUCCESS";
    public const string Failed = "FAIL";
}