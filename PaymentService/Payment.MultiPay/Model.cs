using Newtonsoft.Json;

namespace Payment.MultiPay
{
    internal class WithdrawalResponse : ResponseBase
    {
        [JsonProperty("data")] public ResponseDataBase Data { get; set; }
    }

    internal class ResponseBase
    {
        [JsonProperty("msg")] public string Message { get; set; }
        [JsonProperty("code")] public int Code { get; set; }
    }

    internal class DepositResponse : ResponseBase
    {
        [JsonProperty("data")] public DepositResponseData Data { get; set; }
    }

    internal class ResponseDataBase
    {
        [JsonProperty("status")] public int Status { get; set; }
    }

    internal class DepositResponseData : ResponseDataBase
    {
        [JsonProperty("pay_url")] public string PayUrl { get; set; }
    }
}