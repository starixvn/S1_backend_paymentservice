using Newtonsoft.Json;

namespace Payment.JokPay
{
    internal class WithdrawalResponse : ResponseBase
    {
    }

    internal class ResponseBase
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("msg")] public string Message { get; set; }
    }

    internal class DepositResponse : ResponseBase
    {
        [JsonProperty("detail")] public ResponseDetail Detail { get; set; }
    }

    internal class ResponseDetail
    {
        [JsonProperty("PayURL")] public string PayUrl { get; set; }
    }
}