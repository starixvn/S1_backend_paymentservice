using Newtonsoft.Json;

namespace Payment.CashRuPay
{
    public class CashRuPayPost
    {
        [JsonProperty("agentcode")]
        public string AgentCode { get; set; }

        [JsonProperty("data")]
        public string Data { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }

        [JsonProperty("timestamp")]
        public string TimeStamp { get; set; }
    }

    public class CashRuPayPostData
    {
        [JsonProperty("orderno")]
        public string OrderNo { get; set; }

        [JsonProperty("payamount")]
        public int PayAmount { get; set; }

        [JsonProperty("thirduserid")]
        public string ThirdUserId { get; set; }

        [JsonProperty("trscode")]
        public string TrsCode { get; set; }

        [JsonProperty("callurl")]
        public string CallUrl { get; set; }

        [JsonProperty("thirduserlevel")]
        public int ThirdUserLevel { get; set; }
    }

    public class CreateDeposit
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("orderno")]
        public string OrderNo { get; set; }

        [JsonProperty("payamount")]
        public string PayAmount { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class Response<T>
    {
        [JsonProperty("code")]
        public string Code { get; set; }

        [JsonProperty("data")]
        public T Data { get; set; }

        [JsonProperty("msg")]
        public string Msg { get; set; }
    }

    public class CallBackRequest
    {
        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("customerid")]
        public string CustomerId { get; set; }

        [JsonProperty("sdorderno")]
        public string sdOrderNo { get; set; }

        [JsonProperty("total_fee")]
        public string TotalFee { get; set; }

        [JsonProperty("paytime")]
        public string PayTime { get; set; }

        [JsonProperty("sign")]
        public string Sign { get; set; }
    }

    public class CashRuPayWithdrawalPostData
    {
        [JsonProperty("orderno")]
        public string OrderNo { get; set; }

        [JsonProperty("money")]
        public int Money { get; set; }

        [JsonProperty("notifyUrl")]
        public string NotifyUrl { get; set; }

        [JsonProperty("receiveAccount")]
        public string ReceiveAccount { get; set; }

        [JsonProperty("receiveBanktype")]
        public string ReceiveBanktype { get; set; }

        [JsonProperty("receiveName")]
        public string ReceiveName { get; set; }
    }

    public class CreateWithdrawal
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("orderno")]
        public string OrderNo { get; set; }
    }
}