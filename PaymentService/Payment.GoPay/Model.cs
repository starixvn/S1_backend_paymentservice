using Newtonsoft.Json;

namespace Payment.GoPay;

public class CreatOrderResponse
{
    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("data")]
    public string Data { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}

public class CreatOrderData
{
    [JsonProperty("trade_no")]
    public string TransId { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("out_trade_no")]
    public string ThirdId { get; set; }

    [JsonProperty("uri")]
    public string Url { get; set; }

    [JsonProperty("qrcode")]
    public string QrCode { get; set; }
}

public class DepositCallBackRequest
{
    [JsonProperty("merchantNo")]
    public string MerchantNo { get; set; }
    
    [JsonProperty("outTradeNo")]
    public string TransId { get; set; }
    
    [JsonProperty("orderNo")]
    public string ThirdId { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}

public class CreatPayoutResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("code")]
    public string Code { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
}


public class PayoutCallBackRequest
{
    [JsonProperty("merchantNo")]
    public string MerchantNo { get; set; }
    
    [JsonProperty("outTradeNo")]
    public string TransId { get; set; }
    
    [JsonProperty("orderNo")]
    public string ThirdId { get; set; }

    [JsonProperty("bankCardNumber")]
    public string BankCardNumber { get; set; }

    [JsonProperty("bankCode")]
    public string BankCode { get; set; }

    [JsonProperty("amount")]
    public string Amount { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("timestamp")]
    public string Timestamp { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}