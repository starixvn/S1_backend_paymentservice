using PaymentCore;

namespace Payment.NewXyPay
{
    internal class Definition
    {
        internal class Path
        {
            /// <summary>
            /// 存款
            /// </summary>
            public const string Deposit = "/pay/unifiedorder";
            /// <summary>
            /// 提款
            /// </summary>
            public const string Withdrawal = "/withdraw/unifiedorder";
        }

        private static Dictionary<string, TransStatus> StatusMap =>
            new()
            {
                { CallBackStatus.FailOverTime, TransStatus.Fail },
                { CallBackStatus.Fail, TransStatus.Fail },
                { CallBackStatus.FailReject, TransStatus.Fail },
                { CallBackStatus.FailReviewReject, TransStatus.Fail },
                { CallBackStatus.Pending, TransStatus.Pending},
                { CallBackStatus.WaitingCheck, TransStatus.Pending},
                { CallBackStatus.WaitingReview, TransStatus.Pending},
                { CallBackStatus.WaitingSend, TransStatus.Pending},
                { CallBackStatus.Success, TransStatus.Success }
            };


        private class CallBackStatus
        {
            public const string FailOverTime = "fail_overtime";
            public const string Fail = "fail";
            public const string FailReject = "fail_reject";
            public const string FailReviewReject = "fail_review_reject";
            public const string Pending = "pending";
            public const string WaitingCheck = "waiting_check";
            public const string WaitingReview = "waiting_review";
            public const string WaitingSend = "waiting_send";
            public const string Success = "success";
        }

        public static TransStatus GetTransStatus(string status)
        {
            return StatusMap.GetValueOrDefault(status, TransStatus.Pending);
        }
    }
    internal class CallbackResponse
    {
        public const string Success = "success";
    }
}
