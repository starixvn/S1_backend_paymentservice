using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Payment.BigPayz
{
    internal class BigPayzDefinition
    {
        internal class Path
        {
            public const string Deposit = "/Payin/DepositV2";

            public const string Withdrawal = "/Payout/Withdrawal";
        }
    }
}
