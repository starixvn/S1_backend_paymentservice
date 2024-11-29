using Newtonsoft.Json;

namespace Payment.SmartPay
{
    internal class DepositResponse : SmartResponseBase
    {
        [JsonProperty("detail")] public DepositResponseDetail Detail { get; set; }
    }

    internal class DepositResponseDetail
    {
        [JsonProperty("PayURL")] public string PayUrl { get; set; }
    }

    internal class SmartResponseBase
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("msg")] public string Message { get; set; }
    }

    internal class WithdrawalResponse : SmartResponseBase
    {
    }
}