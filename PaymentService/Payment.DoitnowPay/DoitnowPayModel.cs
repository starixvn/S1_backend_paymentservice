using Newtonsoft.Json;

namespace Payment.DoitnowPay;

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