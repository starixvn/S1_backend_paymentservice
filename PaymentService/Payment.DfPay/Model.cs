using Newtonsoft.Json;

namespace Payment.DfPay
{
    public class PaymentResponse
    {
        [JsonProperty("systate")]
        public string Systate { get; set; }

        [JsonProperty("msg")]
        public string msg { get; set; }

        [JsonProperty("data")]
        public object Data { get; set; }
    }

    public class ReceiveModel
    {
        [JsonProperty("orderno")]
        public string OrderNo { get; set; }

        [JsonProperty("orderstate")]
        public string OrderState { get; set; }

        [JsonProperty("truedfmoney")]
        public string TrueDfMoney { get; set; }

        [JsonProperty("endtime")]
        public string EndTime { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }
    }

}
