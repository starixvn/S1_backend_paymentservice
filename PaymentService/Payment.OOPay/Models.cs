using Newtonsoft.Json;

namespace Payment.OOPay
{
    public class DepositResponse
    {
        [JsonProperty("amount")] 
        public string Amount { get; set; }

        [JsonProperty("channelNo")] 
        public string ChannelNo { get; set; }

        [JsonProperty("code")] 
        public string Code { get; set; }

        [JsonProperty("orderNo")] 
        public string OrderNo { get; set; }

        [JsonProperty("targetUrl")] 
        public string TargetUrl { get; set; }

        [JsonProperty("text")] 
        public string Text { get; set; }

        /// <summary>
        /// OOPay系统的交易单号
        /// </summary>
        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }
    }
    public class WithdrawalResponse
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }

        /// <summary>
        /// OOPay系统的交易单号
        /// </summary>
        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }
    }
}