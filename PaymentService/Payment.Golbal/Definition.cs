using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PaymentCore;

namespace Payment.Golbal
{
    internal class Definition
    {
        public const string Deposit = "/gateway/v1/order";
        public const string Withdrawal = "/gateway/v1/payfor";

        public class Currency
        {
            public const string INR = "INR";
            public const string BRL = "BRL";
        }

        private class GolbalDepositStatus
        {
            public const int Unpaid = 0;
            public const int Paid = 1;
            public const int Closed = 2;
        }

        private class GolbalWithdrawalStatus
        {
            public const int Success = 2;
            public const int Fail = 3;
        }

        private static Dictionary<int, TransStatus> GolbalDepositStatusMap =>
            new()
            {
                { GolbalDepositStatus.Unpaid, TransStatus.Pending },
                { GolbalDepositStatus.Paid, TransStatus.Success },
                { GolbalDepositStatus.Closed, TransStatus.Fail }
            };

        private static Dictionary<int, TransStatus> GolbalWithdrawalStatusMap =>
            new()
            {
                { GolbalDepositStatus.Unpaid, TransStatus.Pending },
                { GolbalDepositStatus.Paid, TransStatus.Success },
                { GolbalDepositStatus.Closed, TransStatus.Fail }
            };

        public static TransStatus GetDepositTransStatus(int status) =>
            GolbalDepositStatusMap.TryGetValue(status, out var result) ? result : TransStatus.Pending;
        public static TransStatus GetWithdrawalTransStatus(int status) =>
            GolbalWithdrawalStatusMap.TryGetValue(status, out var result) ? result : TransStatus.Pending;
    }
}
