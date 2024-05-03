using Newtonsoft.Json;

namespace Payment.MTPay
{
    public class CreatOrderResponse
    {
        [JsonProperty("code")] 
        public bool Success { get; set; }

        [JsonProperty("msg")] 
        public string Memo { get; set; }

        [JsonProperty("payurl")] 
        public string Url { get; set; }

        [JsonProperty("date")] 
        public DateTime QrCode { get; set; }
    }

    public class CreatOrderData
    {
        [JsonProperty("trade_no")] 
        public string TransId { get; set; }

        [JsonProperty("amount")] 
        public string Amount { get; set; }

        [JsonProperty("out_trade_no")] 
        public string ThirdId { get; set; }

        [JsonProperty("uri")] 
        public string Url { get; set; }

        [JsonProperty("qrcode")] 
        public string QrCode { get; set; }
    }

    public class CallBackRequest
    {
        [JsonProperty("code")]
        public int code { get; set; } // 1表示成功，失败不会通知

        [JsonProperty("merchant_num")]
        public string merchantNum { get; set; } // 商户号

        [JsonProperty("coin")]
        public decimal Amount { get; set; } // 支付的金额，单位为分

        [JsonProperty("merchant_order")]
        public string TransId { get; set; } // 你的订单号

        [JsonProperty("serial_number")]
        public string ThirdId { get; set; } // 流水号

        [JsonProperty("order_time")]
        public string orderTime { get; set; } // 订单入库时间

        [JsonProperty("success_time")]
        public string successTime { get; set; } // 订单成功时间

        [JsonProperty("sign")]
        public string Sign { get; set; } // 签名
    }

    public class CreatPayoutResponse
    {
        [JsonProperty("code")] 
        public bool Success { get; set; }

        [JsonProperty("msg")] 
        public string Memo { get; set; }
    }

    public class CreatPayoutData
    {
        [JsonProperty("trade_no")] 
        public string TransId { get; set; }

        [JsonProperty("amount")] 
        public string Amount { get; set; }

        [JsonProperty("out_trade_no")] 
        public string ThirdId { get; set; }

        [JsonProperty("bank_id")] 
        public string BankId { get; set; }

        [JsonProperty("account_number")] 
        public string AccountNumber { get; set; }

        [JsonProperty("bank_owner")] 
        public string BankOwner { get; set; }

        [JsonProperty("callback_url")] 
        public string CallbackUrl { get; set; }

        [JsonProperty("state")] 
        public string State { get; set; }
    }
}