using System.Reflection;
using Newtonsoft.Json;

namespace Payment.NewXyPay
{
    public class PaymentResponse
    {
        [JsonProperty("storeCode")]
        public string StoreCode { get; set; }

        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }

        [JsonProperty("storeOrderNo")]
        public string StoreOrderNo { get; set; }

        [JsonProperty("payResult")]
        public string PayResult { get; set; }

        [JsonProperty("totalAmt")]
        public string TotalAmt { get; set; }

        /// <summary>
        /// only deposit response
        /// </summary>
        [JsonProperty("payUrl")]
        public string PayUrl { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }

        [JsonProperty("random")]
        public string Random { get; set; }

        /// <summary>
        /// 錯誤回應才會有值
        /// </summary>
        [JsonProperty("errorCode")]
        public string ErrorCode { get; set; }

        /// <summary>
        /// 錯誤回應才會有值
        /// </summary>
        [JsonProperty("errorMsg")]
        public string ErrorMsg { get; set; }

        [JsonProperty("payStartTime")]
        public string payStartTime { get; set; }

        [JsonProperty("payEndTime")]
        public string PayEndTime { get; set; }
    }

    public class ReceiveModel
    {
        [JsonProperty("storeCode")]
        public string StoreCode { get; set; }

        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }

        [JsonProperty("storeOrderNo")]
        public string StoreOrderNo { get; set; }

        [JsonProperty("payResult")]
        public string PayResult { get; set; }

        [JsonProperty("totalAmt")]
        public string TotalAmt { get; set; }

        [JsonProperty("payStartTime")]
        public string PayStartTime { get; set; }

        [JsonProperty("payEndTime")]
        public string PayEndTime { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }

        [JsonProperty("random")]
        public string Random { get; set; }
    }

}
