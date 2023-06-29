using Newtonsoft.Json;

namespace Payment.DirePay
{
    internal class CryptoPayoutCallBackReq
    {
        [JsonProperty("txid")]
        public string TransId { get; set; }
        
        [JsonProperty("direpay_id")]
        public string ThirdId { get; set; }
        
        [JsonProperty("tx_hash")]
        public string TxHash { get; set; }
        
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        
        [JsonProperty("token")]
        public string Token { get; set; }
        
        [JsonProperty("address")]
        public string Address { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("fee")]
        public decimal Fee { get; set; }
    }

    internal class FiatPaymentCallBackReq
    {
        [JsonProperty("txid")]
        public string ThirdId { get; set; }
        
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("customer_uid")]
        public string CustomerUid { get; set; }
        
        [JsonProperty("request_amount")]
        public decimal RequestAmount { get; set; }
        
        [JsonProperty("currency")]
        public string Currency { get; set; }
        
        [JsonProperty("actual_amount")]
        public decimal ActualAmount { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("method")]
        public string Method { get; set; }
    }
    
    internal class CryptoPayinCallBackReq
    {
        [JsonProperty("txid")]
        public string ThirdId { get; set; }
        
        [JsonProperty("line_items")]
        public IEnumerable<LineItem> LineItems { get; set; }
        
        [JsonProperty("tx_hash")]
        public string TxHash { get; set; } 
        
        [JsonProperty("invoice_amount")]
        public decimal InvoiceAmount { get; set; }
        
        [JsonProperty("invoice_currency")]
        public string InvoiceCurrency { get; set; }
        
        [JsonProperty("payment_amount")]
        public string PaymentAmount { get; set; }
        
        [JsonProperty("payment_token")]
        public string PaymentToken { get; set; }
        
        [JsonProperty("rates")]
        public string Rates { get; set; }
        
        [JsonProperty("client_reference_id")]
        public string ClientReferenceId { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
        
        [JsonProperty("hash_code")]
        public string HashCode { get; set; }
        
        [JsonProperty("enable_promotion")]
        public bool EnablePromotion { get; set; }
        
        [JsonProperty("promotion_rate")]
        public decimal PromotionRate { get; set; }
        
        [JsonProperty("promotion_fiat_amount")]
        public decimal PromotionFiatAmount { get; set; }
        
        [JsonProperty("paid_fiat")]
        public decimal PaidFiat { get; set; }
    }
    


    internal class PayoutGetWalletResp
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("txid")]
        public string ThirdId { get; set; }
        
        [JsonProperty("amount")]
        public string Amount { get; set; }
        
        [JsonProperty("token")]
        public string Token { get; set; }
        
        [JsonProperty("status")]
        public string Status { get; set; }
    }

    internal class GetAccessTokenResp
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    internal class PayinGetWalletReq
    {
        public string command { get; set; }
        public LineItem[] line_items { get; set; }
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

    internal class FiatPayoutResp
    {
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("txid")]
        public string ThirdId { get; set; }
        
        [JsonProperty("amount")]
        public string Amount { get; set; }
        
        [JsonProperty("currency")]
        public string Currency { get; set; }
        
        [JsonProperty("method")]
        public string Method { get; set; }
    }
    
    internal class ExchangeResp
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }
        
        [JsonProperty("currency")]
        public string Currency { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
