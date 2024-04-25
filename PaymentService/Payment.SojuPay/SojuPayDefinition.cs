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
        private static Dictionary<string, TransStatus> SojuPayStatusMap =>
            new()
            {
                { SojuPayStatus.Reserved, TransStatus.Pending },
                { SojuPayStatus.Pending, TransStatus.Pending },
                { SojuPayStatus.InProgress, TransStatus.Pending },
                { SojuPayStatus.Success, TransStatus.Success },
                { SojuPayStatus.Cancelled, TransStatus.Fail },
                { SojuPayStatus.Failed, TransStatus.Fail }
            };

        public static string CallbackResponse = "Success";

        public static TransStatus GetTransStatus(string status)
            => SojuPayStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;
        private class SojuPayStatus
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
