using PaymentCore;

namespace Payment.CommsPay
{
    internal static class Definition
    {
        internal const string CallBackResponse = "SUCCESS";

        private static Dictionary<string, TransStatus> StatusMap =>
            new()
            {
                { "2", TransStatus.Success },
                { "3", TransStatus.Fail }
            };

        internal static TransStatus GetTransStatus(string status)
            => StatusMap.GetValueOrDefault(status, TransStatus.Pending);
    }
}