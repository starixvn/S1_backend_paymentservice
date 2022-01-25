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
        public line_items[] line_items { get; set; }
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
        public line_items[] line_items { get; set; }
        public string hashCode { get; set; }
        public string currency { get; set; }
        public string token { get; set; }
        public string callback_url { get; set; }
        public string customer_uid { get; set; }
        public string customer_email { get; set; }
        public string client_reference_id { get; set; }
    }

    internal class line_items
    {
        public string name { get; set; }
        public string item_id { get; set; }
        public string description { get; set; }
        public string amount { get; set; }
        public string quantity { get; set; }
    }

    internal class DepositGetWalletResp
    {
        public string message { get; set; }
        public string txid { get; set; }
        public string customer_uid { get; set; }
        public string invoice_amount { get; set; }
        public string invoice_currency { get; set; }
        public string payment_token { get; set; }
        public string rates { get; set; }
        public string payment_address { get; set; }
        public string payment_amount { get; set; }
        public bool enable_promotion { get; set; }
        public string promotion_rate { get; set; }
        public string promotion_fiat_amount { get; set; }
        public string promotion_crypto_amount { get; set; }
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
