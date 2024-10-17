using PaymentCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.Bajicash
{
    public class Definition
    {
        internal class Path
        {
            /// <summary>
            /// 創建代收
            /// </summary>
            public const string Deposit = "v4/generateDeposit";
            /// <summary>
            /// 創建代付
            /// </summary>
            public const string Payout = "api/v1/payout";
        }

        internal class CryptoMethod
        {
            /// <summary>
            /// 加密
            /// </summary>
            public const string Encrypt = "encrypt";
            /// <summary>
            /// 解密
            /// </summary>
            public const string Decrypt = "decrypt";
        }

        private static Dictionary<string, TransStatus> BajicashStatusMap =>
            new()
            {
                { BajicashStatus.Pending, TransStatus.Pending },
                { BajicashStatus.Completed, TransStatus.Success },
                { BajicashStatus.Failed, TransStatus.Fail },
                { BajicashStatus.ManualProcess, TransStatus.Pending },
            };

        public static TransStatus GetTransStatus(string status)
            => BajicashStatusMap.TryGetValue(status, out var result)
                ? result : TransStatus.Pending;

        private class BajicashStatus
        {
            public const string Pending = "1";
            public const string Completed = "2";
            public const string Failed = "3";
            public const string ManualProcess = "4";
        }
    }
}
