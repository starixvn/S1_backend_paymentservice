using Newtonsoft.Json;

namespace Payment.BetcatPay;

public class DepositResponse
{
    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("data")]
    public DepositResponseData Data { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
}

public class DepositResponseData
{
    [JsonProperty("orderStatus")]
    public int Status { get; set; }
    
    [JsonProperty("orderNo")]
    public string ThirdId { get; set; }
    
    [JsonProperty("params")]
    public DepositData Params { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}

public class DepositData
{
    [JsonProperty("url")]
    public string Url { get; set; }
    
    [JsonProperty("qrcode")]
    public string QrCode { get; set; }
}

public class CallbackRequest
{
    [JsonProperty("orderStatus")]
    public int Status { get; set; }
    
    [JsonProperty("orderNo")]
    public string ThirdId { get; set; }
    
    [JsonProperty("merOrderNo")]
    public string TransId { get; set; }
    
    [JsonProperty("amount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
    
    [JsonProperty("sign")]
    public string Sign { get; set; }
}

public class WithdrawalRequest
{
    public string AppId { get; set; }
    
    [JsonProperty("merOrderNo")]
    public string TransId { get; set; }
    
    public string Currency { get; set; }
    
    [JsonProperty("amount")]
    public string RequestAmount { get; set; }
    
    [JsonProperty("notifyUrl")]
    public string CallBackUrl { get; set; }
    
    [JsonProperty("extra")]
    public WithdrawalRequestData Data { get; set; }
    
    public string Sign { get; set; }
}

public class WithdrawalRequestData
{
    public string BankCode { get; set; }
    
    public string AccountName { get; set; }
    
    public string AccountNo { get; set; }
}

public class WithdrawalResponse
{
    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("data")]
    public WithdrawalResponseData Data { get; set; }
    
    [JsonProperty("msg")]
    public string Message { get; set; }
}

public class WithdrawalResponseData
{
    
    [JsonProperty("orderStatus")]
    public int Status { get; set; }
    
    [JsonProperty("orderNo")]
    public string ThirdId { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
}