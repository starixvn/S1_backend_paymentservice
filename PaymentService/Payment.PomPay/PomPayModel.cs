using Newtonsoft.Json;

namespace Payment.PomPay;

public class CreateOrderResponse
{
    [JsonProperty("ClientId")]
    public string ClientId { get; set; }
    
    [JsonProperty("transactionId")]
    public string TransId { get; set; }
    
    [JsonProperty("referenceId")]
    public string ReferenceId { get; set; }
    
    [JsonProperty("type")]
    public string Type { get; set; }
    
    [JsonProperty("depositAmount")]
    public string Amount { get; set; }
    
    [JsonProperty("qrString")]
    public string Url { get; set; }
}

public class PayoutCallBackResponse
{
    [JsonProperty("referenceId")]
    public string ReferenceId { get; set; }
    [JsonProperty("transactionId")]
    public string TransId { get; set; }
    [JsonProperty("custName")]
    public string AccountName { get; set; }
    [JsonProperty("custBank")]
    public string BankCode { get; set; }
    [JsonProperty("custBankAcc")]
    public string AccountNo { get; set; }
    [JsonProperty("amount")]
    public string Amount { get; set; }
    [JsonProperty("fee")]
    public string Fee { get; set; }
    [JsonProperty("total")]
    public string Total { get; set; }
    [JsonProperty("status")]
    public string Status { get; set; }
    [JsonProperty("code")]
    public string Code { get; set; }
    [JsonProperty("remark")]
    public string Remark { get; set; }
    [JsonProperty("hashVal")]
    public string Sign { get; set; }
}


public class CallBackRequest
{
    [JsonProperty("out_trade_no")]
    public string TransId { get; set; }
    
    [JsonProperty("amount")]
    public string Amount { get; set; }
    
    [JsonProperty("request_amount")]
    public string RequestAmount { get; set; }
    
    [JsonProperty("trade_no")]
    public string ThirdId { get; set; }
    
    [JsonProperty("state")]
    public string State { get; set; }
    
    [JsonProperty("callback_url")]
    public string CallbackUrl { get; set; }
    
    [JsonProperty("sign")]
    public string Sign { get; set; }
}

public class CreatPayoutResponse
{
    [JsonProperty("success")]
    public bool Success { get; set; }
    
    [JsonProperty("data")]
    public CreatPayoutData Data { get; set; }
}

public class CreatPayoutData
{
    [JsonProperty("trade_no")]
    public string TransId { get; set; }
    
    [JsonProperty("amount")]
    public string Amount { get; set; }
    
    [JsonProperty("out_trade_no")]
    public string ThirdId { get; set; }
    
    [JsonProperty("bank_id")]
    public string BankId { get; set; }
    
    [JsonProperty("account_number")]
    public string AccountNumber { get; set; }
    
    [JsonProperty("bank_owner")]
    public string BankOwner { get; set; }
    
    [JsonProperty("callback_url")]
    public string CallbackUrl { get; set; }
    
    [JsonProperty("state")]
    public string State { get; set; }
}
