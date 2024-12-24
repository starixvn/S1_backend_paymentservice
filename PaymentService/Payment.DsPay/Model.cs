
using Newtonsoft.Json;

namespace Payment.DsPay
{
    public class DepositData
    {
        [JsonProperty("trade_no")]
        public string TradeNo { get; set; }
        [JsonProperty("pay_url")]
        public string PayUrl { get; set; }
    }
    
    public class PaymentResponse<T>
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("msg")]
        public string Msg { get; set; }
        [JsonProperty("data")]
        public T Data { get; set; }
    }
}