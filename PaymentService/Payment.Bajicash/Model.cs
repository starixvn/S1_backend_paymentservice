using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;


namespace Payment.Bajicash
{
    public class PaymentRequest
    {
        [JsonProperty("transaction_amount")]
        public decimal TransactionAmount { get; set; }

        [JsonProperty("payment_code")]
        public string PaymentCode { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonProperty("callback_url")]
        public string CallbackUrl { get; set; }

        [JsonProperty("return_url")]
        public string ReturnUrl { get; set; }

        [JsonProperty("ip_address")]
        public string IPAddress { get; set; }
    }
    public class CallbackRequest
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("transaction_code")]
        public string TransactionCode { get; set; }
        [JsonProperty("transaction_no")]
        public string TransactionNo { get; set; }
        [JsonProperty("user_id")]
        public string UserId { get; set; }
        [JsonProperty("note")]
        public string Note { get; set; }
    }

    public class PaymentResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("success")]
        public bool Success { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("data")]
        public ResponseData Data { get; set; }
    }

    public class ResponseData
    {
        [JsonProperty("transaction_no")]
        public string TransactionNo { get; set; }
        [JsonProperty("page_url")]
        public string PageUrl { get; set; }
        [JsonProperty("additional")]
        public Additional Additional { get; set; }
    }

    public class Additional
    {
        [JsonProperty("additional_data")]
        public object AdditionalData { get; set; } 
    }
}
