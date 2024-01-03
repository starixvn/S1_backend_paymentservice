using PaymentCore;

namespace Payment.TopPay
{
    public class TopPayDefinition
    {
        public const string CallbackResponse = "SUCCESS";

        internal class Path
        {
            /// <summary>
            /// 創建代收
            /// </summary>
            public const string CreateOrder = "/gateway/prepaidOrder";

            /// <summary>
            /// 創建代付
            /// </summary>
            public const string CreatePayout = "/gateway/cash";
        }

        private static Dictionary<string, TransStatus> TransStatusMap =>
            new()
            {
                { PayoutStatus.Completed, TransStatus.Success },
                { PayoutStatus.New, TransStatus.Pending },
                { PayoutStatus.Processing, TransStatus.Pending },
                { PayoutStatus.Failed, TransStatus.Fail },
                { CreditStatus.Completed, TransStatus.Success},
                { CreditStatus.New, TransStatus.Pending},
                { CreditStatus.Processing, TransStatus.Pending},
                { CreditStatus.Failed, TransStatus.Fail},
            };


        public static TransStatus GetTransStatus(string status)
        {
            if (TransStatusMap.TryGetValue(status, out var result))
            {
                return result;
            }

            return TransStatus.Pending;
        }

        private class CreditStatus
        {
            public const string New = "INIT_ORDER";

            public const string Processing = "NO_PAY";

            public const string Completed = "SUCCESS";

            public const string Failed = "PAY_ERROR";

            public const string Cancel = "PAY_CANCEL";
        }

        private class PayoutStatus
        {
            public const string New = "0";

            public const string Processing = "1";

            public const string Completed = "2";

            public const string Failed = "4";
        }
    }

    internal class MediaType
    {
        public const string Json = "application/json";
    }
}
