using Newtonsoft.Json;

namespace Payment.TopPay
{
    public class CreatOrderResponse
    {
        [JsonProperty("platRespCode")]
        public string PlatRespCode { get; set; }
        
        [JsonProperty("platRespMessage")]
        public string PlatRespMessage { get; set; }
        
        [JsonProperty("platSign")] 
        public string PlatSign { get; set; }
        
        [JsonProperty("platOrderNum")]
        public string PlatOrderNum { get; set; }

        [JsonProperty("payMoney")]
        public string PayMoney { get; set; }

        [JsonProperty("payFee")]
        public string PayFee { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("productDetail")]
        public string ProductDetail { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("orderNum")]
        public string OrderNum { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("payData")]
        public string PayData { get; set; }
    }

    public class DebitCallBackRequest
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("platOrderNum")]
        public string PlatOrderNum { get; set; }

        [JsonProperty("orderNum")]
        public string OrderNum { get; set; }

        [JsonProperty("method")]
        public string Method { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("payMoney")]
        public string PayMoney { get; set; }

        [JsonProperty("payFee")]
        public string PayFee { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }

        [JsonProperty("platSign")]
        public string PlatSign { get; set; }
    }

    public class CreatPayoutResponse
    {
        [JsonProperty("platRespCode")]
        public string PlatRespCode { get; set; }

        [JsonProperty("platRespMessage")]
        public string PlatRespMessage { get; set; }

        [JsonProperty("platOrderNum")]
        public string PlatOrderNum { get; set; }

        [JsonProperty("orderNum")]
        public string OrderNum { get; set; }

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("statusMsg")]
        public string StatusMsg { get; set; }

        [JsonProperty("money")]
        public string Money { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }

        [JsonProperty("feeType")]
        public string FeeType { get; set; }

        [JsonProperty("bankCode")]
        public string BankCode { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("platSign")]
        public string PlatSign { get; set; }
    }

    public class PayOutCallBackResponse
    {
        [JsonProperty("platOrderNum")]
        public string PlatOrderNum { get; set; }

        [JsonProperty("orderNum")]
        public string OrderNum { get; set; }

        [JsonProperty("money")]
        public string Money { get; set; }

        [JsonProperty("feeType")]
        public string FeeType { get; set; }

        [JsonProperty("fee")]
        public string Fee { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("number")]
        public string Number { get; set; }

        [JsonProperty("bankCode")]
        public string BankCode { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("statusMsg")]
        public string StatusMsg { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("platSign")]
        public string PlatSign { get; set; }
    }
}