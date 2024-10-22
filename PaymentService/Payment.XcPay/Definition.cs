using PaymentCore;

namespace Payment.XcPay
{
    internal class Definition
    {
        public const string QR = "IndiaQR";
        public const string RequestId = "03b30f8f-8f80-11ef-868a-35ccf23fad77";
        internal class CryptoMethod
        {
            /// <summary>
            /// MD5
            /// </summary>
            public const string MD5 = "MD5";
        }

        private static Dictionary<string, TransStatus> StatusMap =>
            new()
            {
                { XcPayStatus.Unpaid, TransStatus.Pending },
                { XcPayStatus.Paid, TransStatus.Success },
                { XcPayStatus.Faild, TransStatus.Fail}
            };

        private class XcPayStatus
        {
            public const string Unpaid = "0";
            public const string Paid = "1";
            public const string Faild = "4";
        }

        public static TransStatus GetTransStatus(string status)
            => StatusMap.GetValueOrDefault(status, TransStatus.Pending);
    }
}
