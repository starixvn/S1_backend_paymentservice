using Newtonsoft.Json;

namespace Payment.QuickPay
{
    public class CreatOrderResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }
        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }
        [JsonProperty("userNo")]
        public int UserNo { get; set; }
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("channelNo")]
        public int ChannelNo { get; set; }
        [JsonProperty("amount")]
        public double Amount { get; set; }
        [JsonProperty("discount")]
        public int Discount { get; set; }
        [JsonProperty("lucky")]
        public int Lucky { get; set; }
        [JsonProperty("payable")]
        public double Payable { get; set; }
        [JsonProperty("kfNo")]
        public string KfNo { get; set; }
        [JsonProperty("targetUrl")]
        public string TargetUrl { get; set; }
        [JsonProperty("qrcode")]
        public string Qrcode { get; set; }
        [JsonProperty("payeeAccNumber")]
        public string PayeeAccNumber { get; set; }
        [JsonProperty("extra")]
        public string Extra { get; set; }
    }
    public class DepositCallBackRequest
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }
        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }
        [JsonProperty("userNo")]
        public int UserNo { get; set; }
        [JsonProperty("userName")]
        public string UserName { get; set; }
        [JsonProperty("channelNo")]
        public int ChannelNo { get; set; }
        [JsonProperty("amount")] 
        public decimal Amount { get; set; }
        [JsonProperty("discount")]
        public decimal Discount { get; set; }
        [JsonProperty("lucky")]
        public decimal Lucky { get; set; }
        [JsonProperty("paid")]
        public decimal Paid { get; set; }
        [JsonProperty("extra")]
        public string Extra { get; set; }
        [JsonProperty("sign")]
        public string Sign { get; set; }
    }

    public class CreatPayoutResponse
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        [JsonProperty("text")]
        public string Text { get; set; }
        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }
        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
    }

    public class WithdrawalCallBackRequest
    {
        [JsonProperty("status")]
        public string Status { get; set; }
        [JsonProperty("tradeNo")]
        public string TradeNo { get; set; }
        [JsonProperty("orderNo")]
        public string OrderNo { get; set; }
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("bankName")]
        public string BankName { get; set; }
        [JsonProperty("bankAccount")]
        public string BankAccount { get; set; }
        [JsonProperty("bankBranch")]
        public string BankBranch { get; set; }
        [JsonProperty("payAccount")]
        public string PayAccount { get; set; }
        [JsonProperty("memo")]
        public string Memo { get; set; }
        [JsonProperty("mobile")]
        public string Mobile { get; set; }
        [JsonProperty("fee")]
        public decimal Fee { get; set; }
        [JsonProperty("extra")]
        public string Extra { get; set; }
        [JsonProperty("sign")]
        public string Sign { get; set; }
    }
}
