using Newtonsoft.Json;

namespace Payment.PomPay
{
    internal class WithdrawalResponse
    {
        [JsonProperty("status")] public string Status { get; set; }

        [JsonProperty("errorCode")] public string ErrorCode { get; set; }

        [JsonProperty("message")] public string Message { get; set; }

        [JsonProperty("referenceId")] public string ReferenceId { get; set; }
    }

    internal class DepositResponse
    {
        [JsonProperty("errorCode")] public string ErrorCode { get; set; }

        [JsonProperty("message")] public string Message { get; set; }

        [JsonProperty("qrString")] public string QrString { get; set; }

        [JsonProperty("referenceId")] public string ReferenceId { get; set; }
    }
}