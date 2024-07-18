using Newtonsoft.Json;

namespace Payment.PPPay;

public class Response<T>
{
    [JsonProperty("code")]
    public string Code { get; set; }
    
    [JsonProperty("message")]
    public string Message { get; set; }
    
    [JsonProperty("data")]
    public T Data { get; set; }
}

public class CreateDeposit
{
    [JsonProperty("orderId")]
    public string ThirdId { get; set; }
    
    [JsonProperty("paymentLink")]
    public string Url { get; set; }
}

public class CreateWithdrawal
{
    [JsonProperty("merchantOrderId")]
    public string TransId { get; set; }
    
    [JsonProperty("orderId")]
    public string ThirdId { get; set; }
}

public class DepositCallbackRequest
{
    [JsonProperty("merchantOrderId")]
    public string TransId { get; set; }
    
    [JsonProperty("orderId")]
    public string ThirdId { get; set; }
    
    [JsonProperty("realAmount")]
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
    [JsonProperty("merchantOrderId")]
    public string TransId { get; set; }
    
    [JsonProperty("orderId")]
    public string ThirdId { get; set; }
    
    [JsonProperty("remittanceAmount")]
    public decimal Amount { get; set; }
    
    [JsonProperty("status")]
    public int Status { get; set; }
    
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
