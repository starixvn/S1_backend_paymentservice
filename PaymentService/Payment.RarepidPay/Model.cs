using Newtonsoft.Json;

namespace Payment.RarepidPay
{
    public class WithdrawalResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; }
    }

    public class WithdrawalPost
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("http_post_url")]
        public string HttpPostUrl { get; set; }

        [JsonProperty("ifsc_code")]
        public string IfscCode { get; set; }

        [JsonProperty("remark")]
        public string Remark { get; set; }

        [JsonProperty("to_account_name")]
        public string ToAccountName { get; set; }

        [JsonProperty("to_bank_account")]
        public string ToBankAccount { get; set; }

        [JsonProperty("to_bank_code")]
        public string ToBankCode { get; set; }

        [JsonProperty("tran_id")]
        public string TranId { get; set; }
    }

    public class WithdrawalCallBackRequest
    {
        [JsonProperty("tran_id")]
        public string TranId { get; set; }

        [JsonProperty("payout_tran_id")]
        public string PayoutTranId { get; set; }

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; }

        [JsonProperty("payout_status")]
        public string PayoutStatus { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("created_at")]
        public string CreatedAt { get; set; }
    }

    public class DepositCallBackRequest
    {
        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("from_bank_code")]
        public string FromBankCode { get; set; }

        [JsonProperty("pg_tran_id")]
        public string PgTranId { get; set; }

        [JsonProperty("signature")]
        public string Signature { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("tran_id")]
        public string TranId { get; set; }
    }

    public class DepositResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; }

        [JsonProperty("pg_tran_id")]
        public string PgTranId { get; set; }

        [JsonProperty("redirect_to")]
        public string RedirectTo { get; set; }
    }

    public class DepositPost
    {
        [JsonProperty("http_post_url")]
        public string HttpPostUrl { get; set; }

        [JsonProperty("return_url")]
        public string ReturnUrl { get; set; }

        [JsonProperty("failed_return_url")]
        public string FailedReturnUrl { get; set; }

        [JsonProperty("depositer")]
        public string Depositer { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("tran_id")]
        public string TranId { get; set; }

        [JsonProperty("remark")]
        public string Remark { get; set; }
    }
}