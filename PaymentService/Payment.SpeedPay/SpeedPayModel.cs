using Newtonsoft.Json;

namespace Payment.SpeedPay;

public class SpeedPaySetting
{
    public string Key { get; set; }
    public string MerchantId { get; set; }
    public string ApiUrl { get; set; }
}

public interface ICreateTransResp
{
    public string Code { get; set; }
    public string Message { get; set; }
    public string ThirdId { get; set; }
    public string TransId { get; set; }
}

public class DepositResp : ICreateTransResp
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }

    [JsonProperty("gbpOrderNo")]
    public string ThirdId { get; set; }

    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("resultUrl")]
    public string ResultUrl { get; set; }

    [JsonProperty("resultQRImage")]
    public string ResultQrImage { get; set; }
}

public class PayoutResp : ICreateTransResp
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("msg")]
    public string Message { get; set; }

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("fee")]
    public decimal Fee { get; set; }

    [JsonProperty("gbpOrderNo")]
    public string ThirdId { get; set; }

    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("createTime")]
    public string CreateTime { get; set; }
}

public class CallbackReq
{
    public string signMsg { get; set; }
    public string version { get; set; }
    public string notifyTime { get; set; }
    public string content { get; set; }
}

public class CallbackContent
{
    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("amount")]
    public decimal Amount { get; set; }

    [JsonProperty("orderNo")]
    public string TransId { get; set; }

    [JsonProperty("gbpOrderNo")]
    public string ThirdId { get; set; }
}

