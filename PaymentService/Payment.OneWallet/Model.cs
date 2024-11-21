using Newtonsoft.Json;

namespace Payment.OneWallet
{
    internal class WithdrawalCallBackRequest
    {
        [JsonProperty("sh_order_no")]
        public string ShOrderNo { get; set; }
        [JsonProperty("order_status")]
        public string OrderStatus { get; set; }
        [JsonProperty("order_amount")]
        public string OrderAmount { get; set; }
        [JsonProperty("paid_amount")]
        public string PaidAmount { get; set; }
    }

    internal class WithdrawalResponse
    {
        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }
        [JsonProperty("data")]
        public WithdrawalResponseData Data { get; set; }
    }

    internal class WithdrawalResponseData
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }
        [JsonProperty("sh_order_no")]
        public string ShOrderNo { get; set; }
    }

    internal class WithdrawalPost
    {
        [JsonProperty("sh_order_no", Order = 1)]
        public string ShOrderNo { get; set; }

        [JsonProperty("bank_code", Order = 2)]
        public string BankCode { get; set; }

        [JsonProperty("bank_account", Order = 3)]
        public string BankAccount { get; set; }

        [JsonProperty("bank_user_name", Order = 4)]
        public string BankUserName { get; set; }

        [JsonProperty("order_amount", Order = 5)]
        public decimal OrderAmount { get; set; }

        [JsonProperty("notify_url", Order = 6)]
        public string NotifyUrl { get; set; }

        [JsonProperty("operate_type", Order = 7)]
        public string OperateType { get; set; }

        [JsonProperty("note", Order = 8, NullValueHandling = NullValueHandling.Ignore)]
        public string Note { get; set; }
    }

    internal class DepositCallBackRequest
    {
        [JsonProperty("sh_order_no")]
        public string ShOrderNo { get; set; }
        [JsonProperty("order_status")]
        public string OrderStatus { get; set; }
        [JsonProperty("order_amount")]
        public string OrderAmount { get; set; }
        [JsonProperty("paid_amount")]
        public string PaidAmount { get; set; }
        [JsonProperty("request_amount")]
        public string RequestAmount { get; set; }
    }

    internal class DepositResponse
    {
        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }
        [JsonProperty("data")]
        public DepositResponseData Data { get; set; }
    }

    internal class RawModel
    {
        [JsonProperty("iv")]
        public string Iv { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
        [JsonProperty("error_code")]
        public string ErrorCode { get; set; }
        [JsonProperty("data")]
        public RawModelData Data { get; set; }
    }

    internal class RawModelData
    {
        [JsonProperty("message")]
        public string Message;
    }

    internal class DepositResponseData
    {
        [JsonProperty("order_id")]
        public string OrderId { get; set; }
        [JsonProperty("order_amount")]
        public string OrderAmount { get; set; }
        [JsonProperty("request_amount")]
        public string RequestAmount { get; set; }
        [JsonProperty("url")]
        public string[] Url { get; set; }
        [JsonProperty("token")]
        public string Token { get; set; }
        [JsonProperty("message")]
        public string Message { get; set; }
    }

    internal class AesModel
    {
        public string iv { get; set; }
        public string value { get; set; }
    }

    internal class DepositPost
    {
        [JsonProperty("sh_order_no", Order = 1)]
        public string ShOrderNo { get; set; }

        [JsonProperty("order_amount", Order = 2)]
        public decimal OrderAmount { get; set; }

        [JsonProperty("order_type", Order = 3)]
        public string OrderType { get; set; }

        [JsonProperty("bank_code", Order = 4)]
        public string? BankCode { get; set; }

        [JsonProperty("notify_url", Order = 5)]
        public string NotifyUrl { get; set; }

        [JsonProperty("note", Order = 6)]
        public string Note { get; set; }
    }
}