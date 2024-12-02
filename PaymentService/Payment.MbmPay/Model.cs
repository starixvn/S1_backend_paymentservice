using Newtonsoft.Json;

namespace Payment.MbmPay
{
    internal class WithdrawalResponse : MbmPayResponseBase
    {
    }

    internal class MbmPayResponseBase
    {
        [JsonProperty("msg")] public string Message { get; set; }

        [JsonProperty("code")] public string Code { get; set; }
    }

    internal class DepositResponse : MbmPayResponseBase
    {
        [JsonProperty("data")] public string Data { get; set; }
    }
}