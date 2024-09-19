using PaymentCore;

namespace Payment.RarepidPay
{
    public class Definition
    {
        public const string SuccessCode = "0";
        public const string CallbackResponse = "OK";

        public static TransStatus GetTransStatus(string status)
            => TransStatusMap.GetValueOrDefault(status, TransStatus.Pending);

        public static Dictionary<string, TransStatus> TransStatusMap =>
            new()
            {
                { "8", TransStatus.Success },
                { "-1", TransStatus.Fail }
            };
    }

    internal class Path
    {
        public const string CreateDeposit = "/Api/QRDeposit";
        public static string CreateWithdrawal = "/Api/Withdraw";
    }
}