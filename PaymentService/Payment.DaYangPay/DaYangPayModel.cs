using Newtonsoft.Json;

namespace Payment.DaYangPay
{
    public class CreatOrderResponse
    {
        [JsonProperty("client_key")] 
        public string ClientKey { get; set; }
        [JsonProperty("amount")] 
        public decimal Amount { get; set; }
        [JsonProperty("trade_no")] 
        public string TradeNo { get; set; }
        [JsonProperty("out_trade_no")] 
        public string OutTradeNo { get; set; }
        [JsonProperty("created_at")] 
        public string CreatedAt { get; set; }
        [JsonProperty("payment_url")] 
        public string PaymentUrl { get; set; }
    }
    
    
    public class CallBackDebitRequest
    {
        [JsonProperty("client_key")] 
        public string ClientKey { get; set; }
        [JsonProperty("signature")] 
        public string Signature { get; set; }
        [JsonProperty("amount")] 
        public decimal Amount { get; set; }
        [JsonProperty("channel_id")] 
        public string Channelid { get; set; }
        [JsonProperty("trade_no")] 
        public string TradeNo { get; set; }
        [JsonProperty("out_trade_no")] 
        public string OutTradeNo { get; set; }
        [JsonProperty("created_at")] 
        public string CreatedAt { get; set; }
        [JsonProperty("paid_at")] 
        public string PaidAt { get; set; }
        [JsonProperty("status")] 
        public int Status { get; set; }
    }

    public class CreatPayoutResponse
    {
        [JsonProperty("client_key")] 
        public string ClientKey { get; set; }
        [JsonProperty("amount")] 
        public decimal Amount { get; set; }
        [JsonProperty("transfer_no")] 
        public string TransferNo { get; set; }
        [JsonProperty("out_transfer_no")] 
        public string OutTransferNo { get; set; }
        [JsonProperty("channel_id")] 
        public string Channelid { get; set; }
        [JsonProperty("payee_account")] 
        public string PayeeAccount { get; set; }
        [JsonProperty("payee_name")] 
        public string PayeeName { get; set; }
        [JsonProperty("created_at")] 
        public string CreatedAt { get; set; }
    }
    
    public class CallBackCreditRequest
    {
        [JsonProperty("client_key")]
        public string ClientKey { get; set; }
        [JsonProperty("signature")]
        public string Signature { get; set; }
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        [JsonProperty("channel_id")]
        public string ChannelId { get; set; }
        [JsonProperty("transfer_no")]
        public string TransferNo { get; set; }
        [JsonProperty("out_transfer_no")]
        public string OutTransferNo { get; set; }
        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
        [JsonProperty("paid_at")]
        public string PaidAt { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
        [JsonProperty("status")]
        public int Status { get; set; }
    }
}