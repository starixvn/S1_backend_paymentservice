using Newtonsoft.Json;

namespace Payment.BCOTC
{
    public interface ISign
    {
        string ApiKey { get; set; }

        string Md5Sign { get; set; }
    }

    public class DepositPost : ISign
    {
        [JsonProperty("amount", Order = 1)]
        public string Amount { get; set; }

        [JsonProperty("api_key", Order = 2, NullValueHandling = NullValueHandling.Ignore)]
        public string ApiKey { get; set; }

        [JsonProperty("apply_timestamp", Order = 3)]
        public double ApplyTimeStamp { get; set; }

        [JsonProperty("md5_sign", NullValueHandling = NullValueHandling.Ignore)]
        public string Md5Sign { get; set; }

        [JsonProperty("merchant_id", Order = 4)]
        public string MerchantId { get; set; }

        [JsonProperty("merchant_order_id", Order = 5)]
        public string MerchantOrderId { get; set; }

        [JsonProperty("notify_url", Order = 6)]
        public string NotifyUrl { get; set; }

        [JsonProperty("payer", Order = 7)]
        public string Payer { get; set; }

        [JsonProperty("payment_method", Order = 8)]
        public int PaymentMethod { get; set; }
    }

    public class WithdrawalPost : ISign
    {
        [JsonProperty("account_name", Order = 1)]
        public string AccountName { get; set; }

        [JsonProperty("account_number", Order = 2)]
        public string AccountNumber { get; set; }

        [JsonProperty("amount", Order = 3)]
        public string Amount { get; set; }

        [JsonProperty("api_key", Order = 4, NullValueHandling = NullValueHandling.Ignore)]
        public string ApiKey { get; set; }

        [JsonProperty("apply_timestamp", Order = 5)]
        public double ApplyTimeStamp { get; set; }

        [JsonProperty("bank_branch", Order = 6)]
        public string BankBranch { get; set; }

        [JsonProperty("bank_name", Order = 7)]
        public string BankName { get; set; }

        [JsonProperty("md5_sign", NullValueHandling = NullValueHandling.Ignore)]
        public string Md5Sign { get; set; }

        [JsonProperty("merchant_id", Order = 8)]
        public string MerchantId { get; set; }

        [JsonProperty("merchant_order_id", Order = 9)]
        public string MerchantOrderId { get; set; }

        [JsonProperty("notify_url", Order = 10)]
        public string NotifyUrl { get; set; }

        [JsonProperty("payment_method", Order = 11)]
        public int PaymentMethod { get; set; }
    }

    public class PaymentResponse
    {
        [JsonProperty("status")] 
        public string Status { get; set; }

        [JsonProperty("msg")] 
        public object Msg { get; set; }

        [JsonProperty("order_id")] 
        public string OrderId { get; set; }

        [JsonProperty("payment_url")] 
        public string PaymentUrl { get; set; }
    }

    public class PaymentReceive : ISign
    {
        [JsonProperty("amount", Order = 1)] 
        public string Amount { get; set; }

        [JsonProperty("api_key", Order = 2, NullValueHandling = NullValueHandling.Ignore)]
        public string ApiKey { get; set; }

        [JsonProperty("completed_at", Order = 3)]
        public decimal? CompletedAt { get; set; }

        [JsonProperty("crypto_amount", Order = 4)]
        public string CryptoAmount { get; set; }

        [JsonProperty("md5_sign", NullValueHandling = NullValueHandling.Ignore)]
        public string Md5Sign { get; set; }

        [JsonProperty("merchant_id", Order = 5)]
        public string MerchantId { get; set; }

        [JsonProperty("merchant_order_id", Order = 6)]
        public string MerchantOrderId { get; set; }

        [JsonProperty("order_id", Order = 7)]
        public string OrderId { get; set; }

        [JsonProperty("status", Order = 8)]
        public string Status { get; set; }
    }
}