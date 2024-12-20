using Newtonsoft.Json;

namespace Payment.RarepidPay
{
    public class PaymentResponse
    {
        [JsonProperty("statuscode")] 
        public int StatusCode { get; set; }
        [JsonProperty("message")] 
        public string Message { get; set; }
        [JsonProperty("url")] 
        public string Url { get; set; }
    }

    public class PaymentRequest
    {
        [JsonProperty("name")] 
        public string Name { get; set; }
        [JsonProperty("amount")] 
        public int Amount { get; set; }
        [JsonProperty("txnid")] 
        public string TxnId { get; set; }
        [JsonProperty("callback_url")] 
        public string CallbackUrl { get; set; }
    }

    public class DepositReceive
    {
        [JsonProperty("statuscode")] 
        public int StatusCode { get; set; }
        [JsonProperty("message")] 
        public string Message { get; set; }
        [JsonProperty("token")] 
        public string Token { get; set; }
        [JsonProperty("amount")] 
        public int Amount { get; set; }
        [JsonProperty("txnid")] 
        public string TxnId { get; set; }
        [JsonProperty("status")] 
        public string Status { get; set; }
        [JsonProperty("signature")] 
        public string Signature { get; set; }
        [JsonProperty("updated")] 
        public string Updated { get; set; }
    }


}