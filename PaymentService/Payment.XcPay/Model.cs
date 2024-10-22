using Newtonsoft.Json;

namespace Payment.XcPay
{
    public class PaymentResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("params")]
        public string Params { get; set; }
        [JsonProperty("timpstamp")]
        public long Timestamp { get; set; }
    }

    public class PaymentParams
    {
        [JsonProperty("merchant_ref")]
        public string MerchantRef { get; set; }

        [JsonProperty("system_ref")]
        public string SystemRef { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("pay_amount")]
        public string PayAmount { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("success_time")]
        public string SuccessTime { get; set; }

        [JsonProperty("extend_params")]
        public string ExtendParams { get; set; }

        [JsonProperty("product")]
        public string Product { get; set; }

        [JsonProperty("product_ref")]
        public string ProductRef { get; set; }

        [JsonProperty("reason")]
        public string Reason { get; set; }

        [JsonProperty("reversal")]
        public int Reversal { get; set; }

        [JsonProperty("payurl")]
        public string Payurl { get; set; }

        [JsonProperty("qrcode")]
        public string Qrcode { get; set; }

        [JsonProperty("extra")]
        public Extra Extra { get; set; }
    }
    public class Extra
    {
        [JsonProperty("vpa")]
        public string Vpa { get; set; }

        [JsonProperty("remark")]
        public string Remark { get; set; }

        [JsonProperty("upi_link")]
        public string UpiLink { get; set; }
    }

}
