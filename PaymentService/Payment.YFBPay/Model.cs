using Newtonsoft.Json;

namespace Payment.YFBPay
{
    internal class WithdrawalResponse
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("text")] public string Text { get; set; }
        [JsonProperty("tradeNo")] public string TradeNo { get; set; }
    }

    internal class DepositResponse
    {
        [JsonProperty("code")] public string Code { get; set; }
        [JsonProperty("text")] public string Text { get; set; }
        [JsonProperty("targetUrl")] public string TargetUrl { get; set; }
        [JsonProperty("tradeNo")] public string TradeNo { get; set; }
    }
}