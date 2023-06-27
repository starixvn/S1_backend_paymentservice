using Newtonsoft.Json;

namespace Payment.DirePay
{
    internal class CallBackWithdrawReq
    {
        public string txid { get; set; }
        public string tx_hash { get; set; }
        public string amount { get; set; }
        public string token { get; set; }
        public string address { get; set; }
        public string customer_email { get; set; }
        public string client_reference_id { get; set; }
        public string status { get; set; }
        public string fee { get; set; }
        public string fee_usd { get; set; }
        public string hash_code { get; set; }
    }

    internal class CallBackDepositReq
    {
        public string txid { get; set; }
        public LineItem[] line_items { get; set; }
        public string tx_hash { get; set; }
        public string customer { get; set; }
        public string customer_email { get; set; }
        public string invoice_amount { get; set; }
        public string invoice_currency { get; set; }
        public string payment_amount { get; set; }
        public string payment_token { get; set; }
        public string rates { get; set; }
        public string client_reference_id { get; set; }
        public string status { get; set; }
        public string hash_code { get; set; }
        public string enable_promotion { get; set; }
        public string promotion_rate { get; set; }
        public string promotion_fiat_amount { get; set; }
        public string paid_fiat { get; set; }
    }

    internal class WithdrawGetWalletReq
    {
        public string command { get; set; }
        public string hashCode { get; set; }
        public string amount { get; set; }
        public string currency { get; set; }
        public string withdrawal_amount { get; set; }
        public string withdrawal_token { get; set; }
        public string address { get; set; }
        public string callback_url { get; set; }
        public string customer_uid { get; set; }
        public string client_reference_id { get; set; }
    }

    internal class WithdrawGetWalletResp
    {
        public string message { get; set; }
        public string txid { get; set; }
        public string amount { get; set; }
        public string token { get; set; }
        public string status { get; set; }
    }

    internal class DepositResp
    {
        public string access_token { get; set; }
        public string message { get; set; }
    }

    internal class DepositGetWalletReq
    {
        public string command { get; set; }
        public IEnumerable<LineItem> line_items { get; set; }
        public string hashCode { get; set; }
        public string currency { get; set; }
        public string token { get; set; }
        public string callback_url { get; set; }
        public string customer_uid { get; set; }
        public string customer_email { get; set; }
        public string client_reference_id { get; set; }
    }

    internal class LineItem
    {
        public string name { get; set; }
        public string item_id { get; set; }
        public string description { get; set; }
        public string amount { get; set; }
        public string quantity { get; set; }
    }

    internal class FiatPayinResp
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("txid")]
        public string ThirdId { get; set; }
        
        [JsonProperty("amount")]
        public string Amount { get; set; }
        
        [JsonProperty("currency")]
        public string Currency { get; set; }
        
        [JsonProperty("pay_url")]
        public string PayUrl { get; set; }
        
        [JsonProperty("auth_code")]
        public string AuthCode { get; set; }
        
        [JsonProperty("method")]
        public string Method { get; set; }
    }
    
    internal class CryptoPayinResp
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("txid")]
        public string ThirdId { get; set; }
        
        [JsonProperty("customer_uid")]
        public string CustomerUid { get; set; }
        
        [JsonProperty("invoice_amount")]
        public string InvoiceAmount { get; set; }
        
        [JsonProperty("invoice_currency")]
        public string InvoiceCurrency { get; set; }
        
        [JsonProperty("payment_token")]
        public string PaymentToken { get; set; }
        
        [JsonProperty("rates")]
        public string Rates { get; set; }
        
        [JsonProperty("payment_address")]
        public string PaymentAddress { get; set; }
        
        [JsonProperty("payment_amount")]
        public string PaymentAmount { get; set; }
        
        [JsonProperty("enable_promotion")]
        public bool EnablePromotion { get; set; }
        
        [JsonProperty("promotion_rate")]
        public string PromotionRate { get; set; }
        
        [JsonProperty("promotion_fiat_amount")]
        public string PromotionFiatAmount { get; set; }
        
        [JsonProperty("promotion_crypto_amount")]
        public string PromotionCryptoAmount{ get; set; }
    }

    internal class ExchangeReq
    {
        public string command { get; set; }
        public string amount { get; set; }
        public string from_currency { get; set; }
        public string to_currency { get; set; }
        public string type { get; set; }
    }
    internal class ExchangeResp
    {
        public string amount { get; set; }
        public string currency { get; set; }
        public string message { get; set; }
    }
}
