using Newtonsoft.Json;

namespace Payment.OnePay;


internal class MediaType
{
    public const string Json = "application/json";
}
/// <summary>
/// 存款(Credit)、提款(Debit) 共用建單回覆
/// </summary>
/// <typeparam name="T"></typeparam>
public class OnePayCreateResponse<T>
{
    [JsonProperty("code")]
    public string? Code { get; set; }

    [JsonProperty("message")]
    public string? Message { get; set; }

    [JsonProperty("data")]
    public T Data { get; set; }
}

/// <summary>
/// 存款(Credit) Data 回覆
/// </summary>
public class OnePayCreateCreditResponse
{
    /// <summary>
    /// 收银台地址，测试通道没有真实地址，正式通道才有实际地址
    /// </summary>
    [JsonProperty("paymentUrl")]
    public string? PaymentUrl { get; set; }
    /// <summary>
    /// 系统订单号,自助回调用此订单号查询
    /// </summary>
    [JsonProperty("channelNo")]
    public string? ChannelNo { get; set; }
    /// <summary>
    /// 商户提交的订单号
    /// </summary>
    [JsonProperty("merchantNo")]
    public string? MerchantNo { get; set; }

}

/// <summary>
/// 存款(Credit)、提款(Debit) 共用回調請求
/// </summary>
public class OnePayCallBackRequest
{
    /// <summary>
    /// AES加秘密文
    /// </summary>
    public string? data { get; set; }
    /// <summary>
    /// 商户订单号
    /// </summary>
    public string? merchantNo { get; set; }
}

/// <summary>
/// 存款(Credit)、提款(Debit) Data 回覆
/// </summary>
public class PlainCallBackData
{
    /// <summary>
    /// 系统订单号
    /// </summary>
    public string? channelNo { get; set; }

    /// <summary>
    /// 商户订单号
    /// </summary>
    public string? merchantNo { get; set; }

    /// <summary>
    /// 金额（单位分）
    /// </summary>
    public int amount { get; set; }

    /// <summary>
    /// 1=进行中,2=成功,3=失败
    /// </summary>
    public int status { get; set; }

    /// <summary>
    /// 订单类型 1=入款，2=出款
    /// </summary>
    public int orderType { get; set; }

    /// <summary>
    /// 失败原因
    /// </summary>
    public string? remark { get; set; }
}

internal class CallbackResponse
{
    public const string Success = "success";
    public const string Failed = "failed";
}
