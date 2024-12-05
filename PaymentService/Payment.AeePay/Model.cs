using Newtonsoft.Json;

namespace Payment.AeePay
{
    public class AeePayResponse<T>
    {
        [JsonProperty("success")]
        public bool Success { get; set; }
        
        [JsonProperty("resultCode")]
        public string ResultCode { get; set; }
        
        [JsonProperty("resultMsg")]
        public string ResultMsg { get; set; }
        
        [JsonProperty("data")]
        public T Data { get; set; }
    }

    public class CreateOrderData
    {
        [JsonProperty("payurl")]
        public string PayUrl { get; set; }
    }

    public class CreatePayoutData
    {
        [JsonProperty("orderno")]
        public string OrderNo { get; set; }
    }
}
