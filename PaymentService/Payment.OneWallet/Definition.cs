using Newtonsoft.Json;
using PaymentCore;

namespace Payment.OneWallet
{
    internal class Definition
    {
        public static string SuccessCode = "200";

        private static Dictionary<string, TransStatus> StatusMap =>
            new()
            {
                { "3", TransStatus.Success },
                { "5", TransStatus.Fail }
            };

        public static TransStatus GetTransStatus(string status)
            => StatusMap.GetValueOrDefault(status, TransStatus.Pending);
    }

    internal class CallbackResponse
    {
        [JsonProperty("result")]
        public string Result { get; set; } = "success";
    }

    internal class Path
    {
        public const string Deposit = "/SH/sendPay";
        public const string Withdrawal = "/SH/sendWithdraw";
    }
}
