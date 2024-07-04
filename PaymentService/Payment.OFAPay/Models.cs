using Newtonsoft.Json;

namespace Payment.OFAPay;

public class DepositResponse
{
    [JsonProperty("statusCode")]
    public string StatusCode { get; set; }
    
    [JsonProperty("payurl")]
    public string Url { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}

public class WithdrawalResponse
{
    [JsonProperty("errcode")]
    public string Code { get; set; }
    
    [JsonProperty("prc")]
    public string Status { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
    
    [JsonProperty("orderno")]
    public string ThirdId { get; set; }
}

public class DepositCallbackRequest
{
    [JsonProperty("respcode")]
    public string Code { get; set; }
    
    [JsonProperty("orderid")]
    public string TransId { get; set; }
    
    [JsonProperty("orderno")]
    public string ThirdId { get; set; }
    
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonProperty("sign")]
    public string Sign { get; set; }
}

public class DepositCallbackResponse
{
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonProperty("qrCodeTransactionId")]
    public string TransId { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}

public class PayoutCallbackRequest
{
    [JsonProperty("respcode")]
    public string Code { get; set; }

    [JsonProperty("orderid")]
    public string TransId { get; set; }

    [JsonProperty("orderno")]
    public string ThirdId { get; set; }

    [JsonProperty("money")]
    public decimal Amount { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("sign")]
    public string Sign { get; set; }
}

public class PayoutCallbackResponse
{
    [JsonProperty("status")]
    public int Status { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}
