using Newtonsoft.Json;

namespace Payment.BixinPay
{
    public class PaymentResponse
    {
        [JsonProperty("responseContent")]
        public Content ResponseContent { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }
    }

    public class Content
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("merchno")]
        public string MerchNo { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("orderId")]
        public string OrderId { get; set; }

        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("timestamp")]
        public string TimeStamp { get; set; }
    }
}
