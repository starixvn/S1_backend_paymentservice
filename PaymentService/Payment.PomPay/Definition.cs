using PaymentCore;

namespace Payment.PomPay
{
    internal class Definition
    {
        internal const string SuccessCode = "";
        internal const string CallbackResponse = "ok";
        internal const string CreditSuccessStatus = "success";

        internal class Path
        {
            internal const string Deposit = "/v2/api/payment";
            internal const string Withdrawal = "/v2/payout";
        }

        internal static Dictionary<string, List<string>> HashKeys =>
            new()
            {
                { "Debit",
                    new List<string>
                    {
                        "transactionId",
                        "custName",
                        "custSecondaryName",
                        "custBank",
                        "custMobile",
                        "custEmail",
                        "amount",
                        "returnUrl",
                        "callbackUrl",
                        "paymentMethod",
                        "bankAcc"
                    }
                },
                { "CallBackDebit",
                    new List<string>
                    {
                        "referenceId",
                        "transactionId",
                        "custName",
                        "amount",
                        "status"
                    }
                },
                { "Credit",
                    new List<string>
                    {
                        "transactionId",
                        "custName",
                        "custBank",
                        "custBankAcc",
                        "custMobile",
                        "custEmail",
                        "amount",
                        "callbackUrl"
                    }
                },
                { "CallBackCredit",
                    new List<string>
                    {
                        "transactionId",
                        "referenceId",
                        "custName",
                        "custBank",
                        "custBankAcc",
                        "amount",
                        "status"
                    }
                }
            };

        private static Dictionary<string, TransStatus> WithdrawalStatusMap =>
            new()
            {
                { "success", TransStatus.Success },
                { "rejected", TransStatus.Fail },
                { "cancelled", TransStatus.Fail }
            };

        public static TransStatus GetDepositTransStatus(string status)
            => status.Equals("success", StringComparison.OrdinalIgnoreCase) ? TransStatus.Success : TransStatus.Fail;

        public static TransStatus GetWithdrawalTransStatus(string status)
            => WithdrawalStatusMap.GetValueOrDefault(status.ToLower(), TransStatus.Pending);
    }
}