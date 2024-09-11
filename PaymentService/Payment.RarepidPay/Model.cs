using Newtonsoft.Json;

namespace Payment.RarepidPay
{
    public class DepositResponse
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("status_message")]
        public string StatusMessage { get; set; }

        [JsonProperty("tran_id")]
        public string TranId { get; set; }

        [JsonProperty("pg_tran_id")]
        public string PgTranId { get; set; }

        [JsonProperty("token")]
        public string Token { get; set; }

        [JsonProperty("amount")]
        public string Amount { get; set; }

        [JsonProperty("total_fees")]
        public string TotalFees { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("redirect_to")]
        public string RedirectTo { get; set; }

    }

    public class DepositRequest
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
        public int Amount { get; set; }

        [JsonProperty("tran_id")]
        public string TranId { get; set; }

        [JsonProperty("remark")]
        public string Remark { get; set; }
    }
}