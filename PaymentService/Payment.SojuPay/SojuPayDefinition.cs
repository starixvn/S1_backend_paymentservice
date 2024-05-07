using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PaymentCore;

namespace Payment.SojuPay
{
    internal class SojuPayDefinition
    {
        internal class Path
        {
            public const string Deposit = "/gateway/api/v1/payments";

            public const string Withdrawal = "/gateway/api/v2/payouts";
        }

        internal class ErrorCode
        {
            public const string Success = "0000";
        }

        public static string CallbackResponse = "Success";

        private static Dictionary<string, TransStatus> SojuPayDepositStatusMap =>
            new()
            {
                { SojuPayDepositStatus.Pending, TransStatus.Pending },
                { SojuPayDepositStatus.InProgress, TransStatus.Pending },
                { SojuPayDepositStatus.Success, TransStatus.Success },
                { SojuPayDepositStatus.Cancelled, TransStatus.Fail },
                { SojuPayDepositStatus.Failed, TransStatus.Fail }
            };

        public static TransStatus GetDepositTransStatus(string status)
            => SojuPayDepositStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
        private class SojuPayDepositStatus
        {
            public const string Pending = "0";
            public const string InProgress = "1";
            public const string Success = "2";
            public const string Cancelled = "3";
            public const string Failed = "4";
        }

        private static Dictionary<string, TransStatus> SojuPayWithdrawalStatusMap =>
            new()
            {
                { SojuPayWithdrawalStatus.Reserved, TransStatus.Pending },
                { SojuPayWithdrawalStatus.Pending, TransStatus.Pending },
                { SojuPayWithdrawalStatus.InProgress, TransStatus.Pending },
                { SojuPayWithdrawalStatus.Success, TransStatus.Success },
                { SojuPayWithdrawalStatus.Cancelled, TransStatus.Fail },
                { SojuPayWithdrawalStatus.Failed, TransStatus.Fail }
            };

        public static TransStatus GetWithdrawalTransStatus(string status)
            => SojuPayWithdrawalStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
        private class SojuPayWithdrawalStatus
        {
            public const string Reserved = "0";      
            public const string Pending = "1";       
            public const string InProgress = "2";    
            public const string Success = "3";       
            public const string Cancelled = "4";     
            public const string Failed = "5";        
        }
    }
}
