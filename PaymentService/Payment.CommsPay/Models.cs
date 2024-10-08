using Newtonsoft.Json;

namespace Payment.CommsPay
{
    internal class WithdrawalResponse
    {
        [JsonProperty("success")]
        public bool Success { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }

    internal class WithdrawalPost
    {
        [JsonProperty("merchant_code", Order = 1)]
        public string MerchantCode { get; set; }

        [JsonProperty("transaction_code", Order = 2)]
        public string TransactionCode { get; set; }

        [JsonProperty("transaction_timestamp", Order = 3)]
        public string TransactionTimestamp { get; set; }

        [JsonProperty("payout_code", Order = 4)]
        public string PayoutCode { get; set; }

        [JsonProperty("transaction_amount", Order = 5)]
        public double TransactionAmount { get; set; }

        [JsonProperty("user_id", Order = 6)]
        public string UserId { get; set; }

        [JsonProperty("currency_code", Order = 7)]
        public string CurrencyCode { get; set; }

        [JsonProperty("bank_account_number", Order = 10)]
        public string BankAccountNumber { get; set; }

        [JsonProperty("ifsc_code", Order = 11)]
        public string IfscCode { get; set; }

        [JsonProperty("bank_code", Order = 12)]
        public string BankCode { get; set; }

        [JsonProperty("bank_name", Order = 14)]
        public string BankName { get; set; }

        [JsonProperty("account_name", Order = 15)]
        public string AccountName { get; set; }

        [JsonProperty("callback_url", Order = 16)]
        public string CallbackUrl { get; set; }
    }

    internal class WithdrawalPostRaw
    {
        [JsonProperty("key")]
        public string Key { get; set; }
    }

    internal class CallBackReqRaw
    {
        [JsonProperty("key")]
        public string Key { set; get; }
    
        [JsonProperty("transaction_code")]
        public string TransactionCode { set; get; }
    
        [JsonProperty("transaction_no")]
        public string TransactionNo { set; get; }
    }
}