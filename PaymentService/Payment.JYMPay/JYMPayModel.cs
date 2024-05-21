using Newtonsoft.Json;

namespace Payment.JYMPay
{
    public class CreateOrderResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public JYMPayCreateOrderData Data { get; set; }
    }

    public class JYMPayCreateOrderData
    {
        [JsonProperty("systemOrderId")]
        public string SystemOrderId { get; set; }

        [JsonProperty("upstreamOrderId")]
        public string UpstreamOrderId { get; set; }

        [JsonProperty("upstreamLink")]
        public string UpstreamLink { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("displayAmount")]
        public string DisplayAmount { get; set; }

        [JsonProperty("cardName")]
        public string CardName { get; set; }

        [JsonProperty("cardAccount")]
        public string CardAccount { get; set; }

        [JsonProperty("cardBank")]
        public string CardBank { get; set; }

        [JsonProperty("cardBranch")]
        public string CardBranch { get; set; }
    }

    public class CallBackRequest
    {
        //Debit
        [JsonProperty("payment_id")]
        public string PaymentId { get; set; }

        [JsonProperty("payment_cl_id")]
        public string PaymentClId { get; set; }
        
        //Credit
        [JsonProperty("payout_id")]
        public string PayoutId { get; set; }

        [JsonProperty("payout_cl_id")]
        public string PayoutClId { get; set; }

        [JsonProperty("platform_id")]
        public string PlatformId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("real_amount")]
        public decimal RealAmount { get; set; }

        [JsonProperty("fee")]
        public decimal Fee { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("create_time")]
        public DateTime CreateTime { get; set; }

        [JsonProperty("update_time")]
        public DateTime UpdateTime { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }
    }

    public class CreditCallBackRequest
    {

        [JsonProperty("platform_id")]
        public string PlatformId { get; set; }

        [JsonProperty("amount")]
        public decimal Amount { get; set; }

        [JsonProperty("fee")]
        public decimal Fee { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("create_time")]
        public DateTime CreateTime { get; set; }

        [JsonProperty("update_time")]
        public DateTime UpdateTime { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }
    }

    public class CreatPayoutResponse
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("data")]
        public CreatPayoutResponseData Data { get; set; }
    }
    
    public class CreatPayoutResponseData
    {
        [JsonProperty("systemOrderId")]
        public string SystemOrderId { get; set; }
    }
    
}