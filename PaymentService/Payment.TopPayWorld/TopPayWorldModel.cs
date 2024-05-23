using Newtonsoft.Json;

namespace Payment.TopPayWorld
{
    public class TopPayWorldBase<T>
    {
        [JsonProperty("code")]
        public string Code { get; set; }
        
        [JsonProperty("data")]
        public T Data { get; set; }
        
        [JsonProperty("msg")]
        public string Msg { get; set; }
        
        [JsonProperty("time")]
        public string Time { get; set; }
    }
    
    public class CreatOrderResponse : TopPayWorldBase<CreatOrderDetail>
    {
    }
    
    public class CallBackRequest : TopPayWorldBase<CallBackDetail>
    {
    }
    
    public class CreatOrderDetail
    {
        [JsonProperty("feeAmount")]
        public string FeeAmount { get; set; }
        
        [JsonProperty("payCategory")]
        public string PayCategory { get; set; }
        
        [JsonProperty("merchantCode")] 
        public string MerchantCode { get; set; }
        
        [JsonProperty("paymentValue")]
        public string PaymentValue { get; set; }

        [JsonProperty("paymentType")]
        public string PaymentType { get; set; }

        [JsonProperty("payAmount")]
        public string PayAmount { get; set; }

        [JsonProperty("payType")]
        public string PayType { get; set; }

        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }

        [JsonProperty("outTradeNo")]
        public string OutTradeNo { get; set; }

        [JsonProperty("payProductCode")]
        public string PayProductCode { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
    
    public class CallBackDetail
    {
        [JsonProperty("feeAmount")]
        public string FeeAmount { get; set; }
        
        [JsonProperty("payCategory")]
        public string PayCategory { get; set; }
        
        [JsonProperty("merchantCode")] 
        public string MerchantCode { get; set; }
        
        [JsonProperty("payAmount")]
        public string PayAmount { get; set; }

        [JsonProperty("payType")]
        public string PayType { get; set; }

        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }
        
        [JsonProperty("outTradeNo")]
        public string OutTradeNo { get; set; }

        [JsonProperty("payProductCode")]
        public string PayProductCode { get; set; }

        [JsonProperty("paymentType")]
        public string PaymentType { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("signature")]
        public string Signature { get; set; }
    }
    
}