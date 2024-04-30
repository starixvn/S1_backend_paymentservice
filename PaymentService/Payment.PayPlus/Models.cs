using Newtonsoft.Json;

namespace Payment.PayPlus;

public class PayPlusReq<T>
{
    [JsonProperty("action")]
    public string Action { get; set; }
    
    [JsonProperty("actionValue")]
    public T ActionValue { get; set; }
}

public class DepositReq
{
    [JsonProperty("merchantOrderNo")]
    public string TransId { get; set; }
    
    [JsonProperty("orderAmount")]
    public string Amount { get; set; }
    
    [JsonProperty("callbackUrl")]
    public string CallbackUrl { get; set; }
    
    [JsonProperty("merchantNo")]
    public string MerchantNo { get; set; }
    
    [JsonProperty("currency")]
    public string Currency { get; set; }

    [JsonProperty("timeStamp")] public string TimeStamp
        => DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
    
    [JsonProperty("productDetail")]
    public string UserName { get; set; }
    
    [JsonProperty("payType")]
    public string PayType { get; set; }
    
    [JsonProperty("payModel")]
    public string PayModel { get; set; }

    [JsonProperty("receiptCurrency")] public string ReceiptCurrency => Currency;
}

public class PayoutReq
{
    [JsonProperty("merchantOrderNo")]
    public string TransId { get; set; }
    
    [JsonProperty("orderAmount")]
    public string Amount { get; set; }
    
    [JsonProperty("currency")]
    public string Currency { get; set; }
    
    [JsonProperty("callbackUrl")]
    public string CallbackUrl { get; set; }
    
    [JsonProperty("merchantNo")]
    public string MerchantNo { get; set; }

    [JsonProperty("timeStamp")] public string TimeStamp
        => DateTimeOffset.Now.ToUnixTimeSeconds().ToString();
    
    [JsonProperty("subjectType")]
    public string SubjectType { get; set; }
    
    [JsonProperty("account")]
    public string AccountName { get; set; }
    
    [JsonProperty("accountCode")]
    public string AccountCode { get; set; }

    [JsonProperty("receiptCurrency")] public string ReceiptCurrency => Currency;
}

public class PayPlusOutResp
{
    [JsonProperty("msg")]
    public string Message { get; set; }
    
    [JsonProperty("code")]
    public int Code { get; set; }
    
    [JsonProperty("data")]
    public string Data { get; set; }
}

public class PayPlusResp<T>
{
    [JsonProperty("meta")]
    public PayPlusRespMeta Meta { get; set; }
    [JsonProperty("data")]
    public T Data { get; set; }
}

public class PayPlusRespMeta
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
    
    [JsonProperty("code")]
    public string Code { get; set; }
}

public class DepositResp
{
    [JsonProperty("webUrl")]
    public string Url { get; set; }
}