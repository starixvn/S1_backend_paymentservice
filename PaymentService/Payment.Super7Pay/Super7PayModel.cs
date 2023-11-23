using Newtonsoft.Json;

namespace Payment.Super7Pay
{
    public class Super7PayModel
    {
        public class CreatOrderResponse
        {
            [JsonProperty("success")] 
            public bool Success { get; set; }

            [JsonProperty("data")] 
            public CreatOrderData Data { get; set; }
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
            [JsonProperty("out_trade_no")] 
            public string TransId { get; set; }

            [JsonProperty("amount")] 
            public string Amount { get; set; }

            [JsonProperty("request_amount")] 
            public string RequestAmount { get; set; }

            [JsonProperty("trade_no")] 
            public string ThirdId { get; set; }

            [JsonProperty("state")] 
            public string State { get; set; }

            [JsonProperty("callback_url")] 
            public string CallbackUrl { get; set; }

            [JsonProperty("sign")] 
            public string Sign { get; set; }
        }

        public class CreatPayoutResponse
        {
            [JsonProperty("success")] 
            public bool Success { get; set; }

            [JsonProperty("data")] 
            public CreatPayoutData Data { get; set; }
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
}